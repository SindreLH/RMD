using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AuthDbContext : IdentityDbContext<ApplicationUser>
{
	public AuthDbContext(DbContextOptions options) : base(options)
	{
	}

	protected AuthDbContext()
	{
	}
}