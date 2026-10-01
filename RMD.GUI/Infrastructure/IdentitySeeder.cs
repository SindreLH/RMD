namespace RMD.GUI.Infrastructure
{
	using Microsoft.AspNetCore.Identity;


	public static class IdentitySeeder
	{
		/// <summary>
		/// Creates RMD's sole user on first start if it does not exist yet.
		/// Credentials come from configuration, never from source:
		///   SeedAdmin:Email / SeedAdmin:Password  (user-secrets in Development, app settings or env vars in Azure).
		/// An existing user is never modified; change the password through "Glemt passord".
		/// </summary>
		public static async Task SeedUserAsync(IServiceProvider services)
		{
			using var scope = services.CreateScope();

			var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
			var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
			var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));

			const string adminRole = "Admin";

			//Check and ensure both roles and users exists.
			if (!await roleManager.RoleExistsAsync(adminRole))
			{
				await roleManager.CreateAsync(new IdentityRole(adminRole));
			}

			if (userManager.Users.Any())
			{
				return;
			}

			var adminEmail = config["SeedAdmin:Email"];
			var adminPassword = config["SeedAdmin:Password"];

			if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
			{
				logger.LogWarning("No users exist and SeedAdmin:Email / SeedAdmin:Password are not configured. Nobody can log in until they are set.");
				return;
			}

			var user = new ApplicationUser
			{
				UserName = adminEmail,
				Email = adminEmail,
				EmailConfirmed = true
			};

			var result = await userManager.CreateAsync(user, adminPassword);

			if (!result.Succeeded)
			{
				throw new Exception(
					"Kunne ikke opprette admin-bruker. " + string.Join(", ", result.Errors.Select(e => e.Description)));
			}

			await userManager.AddToRoleAsync(user, adminRole);
			logger.LogInformation("Seeded admin user {Email}", adminEmail);
		}
	}
}
