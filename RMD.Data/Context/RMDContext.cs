using RMD.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace RMD.Data.Context
{
	public class RMDContext : DbContext
	{
		public DbSet<Song> Songs { get; set; }
		public DbSet<Artist> Artists { get; set; }

		// Constructor added for accepting DbContextOptions Configuration
		public RMDContext(DbContextOptions<RMDContext> options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);
		}
	}
}
