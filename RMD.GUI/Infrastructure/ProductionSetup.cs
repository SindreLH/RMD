using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RMD.Data.Context;

namespace RMD.GUI.Infrastructure
{
	/// <summary>
	/// Hosting concerns for running outside the developer machine (Azure App Service, Docker).
	/// Every setting is optional and documented in docs/azure-deployment.md.
	/// </summary>
	public static class ProductionSetup
	{
		/// <summary>
		/// Login cookies and reset tokens are encrypted with Data Protection keys. App Service keeps the keys
		/// for you; other hosts (Docker, a plain VM) need DataProtection:KeysDirectory or every restart logs you out.
		/// </summary>
		public static IServiceCollection AddRmdDataProtection(this IServiceCollection services, IConfiguration config)
		{
			var dataProtection = services.AddDataProtection().SetApplicationName("RMD");

			var keysDirectory = config["DataProtection:KeysDirectory"];
			if (!string.IsNullOrWhiteSpace(keysDirectory))
				dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));

			return services;
		}

		public static IServiceCollection AddRmdHealthChecks(this IServiceCollection services)
		{
			services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
			return services;
		}

		/// <summary>
		/// Hardening headers. The CSP allows what the app actually uses: Blazor's websocket (same origin),
		/// ApexCharts (eval for formatter functions, inline styles), artist pictures from any https host
		/// and the YouTube playlist embed on the Mixtapes page.
		/// </summary>
		public static IApplicationBuilder UseRmdSecurityHeaders(this IApplicationBuilder app)
		{
			const string csp =
				"default-src 'self'; " +
				"script-src 'self' 'unsafe-eval'; " +
				"style-src 'self' 'unsafe-inline'; " +
				"img-src 'self' data: https:; " +
				"font-src 'self' data:; " +
				"media-src 'self'; " +
				"connect-src 'self'; " +
				"frame-src https://www.youtube.com https://www.youtube-nocookie.com; " +
				"frame-ancestors 'none'; " +
				"base-uri 'self'; " +
				"form-action 'self'; " +
				"object-src 'none'";

			return app.Use(async (context, next) =>
			{
				var headers = context.Response.Headers;
				headers["X-Content-Type-Options"] = "nosniff";
				headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
				headers["X-Frame-Options"] = "DENY";
				headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

				// Swagger UI (Development only) relies on inline scripts
				if (!context.Request.Path.StartsWithSegments("/swagger"))
					headers["Content-Security-Policy"] = csp;

				await next();
			});
		}

		/// <summary>
		/// With Database:MigrateOnStartup = true the app creates/updates both schemas before it starts,
		/// which makes the first deploy to an empty Azure SQL database a one-step affair.
		/// </summary>
		public static async Task MigrateDatabasesIfConfiguredAsync(this WebApplication app)
		{
			if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
				return;

			using var scope = app.Services.CreateScope();
			var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");

			var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<RMDContext>>();
			await using (var db = await factory.CreateDbContextAsync())
				await db.Database.MigrateAsync();

			await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();

			logger.LogInformation("Database migrations applied");
		}

		/// <summary>Warns when a non-development host still uses the local developer connection string.</summary>
		public static void WarnAboutLocalConnectionString(this WebApplication app)
		{
			var connectionString = app.Configuration.GetConnectionString("RmdDatabase") ?? "";
			if (!app.Environment.IsDevelopment() &&
				connectionString.Contains("Server=localhost", StringComparison.OrdinalIgnoreCase))
			{
				app.Logger.LogWarning("ConnectionStrings:RmdDatabase still points at localhost. Set it in the host's configuration (Azure: Configuration > Connection strings).");
			}
		}
	}

	/// <summary>Healthy when the database answers. Used by /healthz (Azure App Service Health check).</summary>
	public sealed class DatabaseHealthCheck : IHealthCheck
	{
		private readonly IDbContextFactory<RMDContext> _factory;

		public DatabaseHealthCheck(IDbContextFactory<RMDContext> factory)
		{
			_factory = factory;
		}

		public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
		{
			try
			{
				await using var db = await _factory.CreateDbContextAsync(cancellationToken);
				return await db.Database.CanConnectAsync(cancellationToken)
					? HealthCheckResult.Healthy()
					: HealthCheckResult.Unhealthy("Database not reachable");
			}
			catch (Exception ex)
			{
				return HealthCheckResult.Unhealthy("Database check failed", ex);
			}
		}
	}
}
