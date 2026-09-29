namespace RMD.GUI.Infrastructure
{
	using Microsoft.AspNetCore.Identity;


	public static class IdentitySeeder
	{
		public static async Task SeedUserAsync(IServiceProvider services)
		{
			using var scope = services.CreateScope();

			var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

			//When preparing for production: Move the consts to appsettings.json
			const string adminEmail = "sindrehalsebakk@gmail.com";
			const string adminPassword = "Test1234!";
			const string adminRole = "Admin";


			//Check and ensure both roles and users exists.
			if (!await roleManager.RoleExistsAsync(adminRole))
			{
				await roleManager.CreateAsync(new IdentityRole(adminRole));
			}

			var user = await userManager.FindByNameAsync(adminEmail);
			
			if (user != null)
			{
				return;
			}

			user = new ApplicationUser
			{
				UserName = adminEmail,
				Email = adminEmail,
				EmailConfirmed = true
			};

			var result = await userManager.CreateAsync(user, adminPassword);
			 
			if (!result.Succeeded)
			{
				throw new Exception(
					"Kunne ikke opprette admin-bruker." + string.Join(", ", result.Errors.Select(e => e.Description)));	
			}

			await userManager.AddToRoleAsync(user, adminRole);
		}
	}
}
