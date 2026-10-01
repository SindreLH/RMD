using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace RMD.GUI.Infrastructure
{
	public static class RateLimitPolicies
	{
		public const string Login = "login";
		public const string PasswordReset = "password-reset";

		/// <summary>
		/// Per-IP limits on the anonymous auth endpoints: 10 logins per minute, 5 reset requests per 15 minutes.
		/// Rejected form posts are sent back to the login page with a message.
		/// </summary>
		public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
		{
			return services.AddRateLimiter(options =>
			{
				options.AddPolicy(Login, context => FixedWindowPerIp(context, 10, TimeSpan.FromMinutes(1)));
				options.AddPolicy(PasswordReset, context => FixedWindowPerIp(context, 5, TimeSpan.FromMinutes(15)));

				options.OnRejected = (context, _) =>
				{
					context.HttpContext.Response.Redirect("/login?reason=ratelimited");
					return ValueTask.CompletedTask;
				};
			});
		}

		private static RateLimitPartition<string> FixedWindowPerIp(HttpContext context, int permits, TimeSpan window) =>
			RateLimitPartition.GetFixedWindowLimiter(
				context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
				_ => new FixedWindowRateLimiterOptions
				{
					PermitLimit = permits,
					Window = window,
					QueueLimit = 0
				});
	}
}
