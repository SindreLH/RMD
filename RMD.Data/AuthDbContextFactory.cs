using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;


//EF CLI is not able to build host correctly, Identity and DbContext is registered before config is fully laoded.
//EF CLI needs an explicit way to create AuthDbContext:

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
	public AuthDbContext CreateDbContext(string[] args)
	{
		var configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json")
			.AddJsonFile("appsettings.Development.json", optional: true)
			.Build();

		var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();

		var connectionString = configuration.GetConnectionString("RmdDatabase");

		optionsBuilder.UseSqlServer(connectionString);

		return new AuthDbContext(optionsBuilder.Options);
	}
}
