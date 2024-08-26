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

			// Test seeding
			modelBuilder.Entity<Song>().HasData(
				new Song { 
					SongId=1, 
					Title="Hands Up Track (Test Remix)", 
					RemixArtist="Test",
					Artist="Artist",
					Length="02:23",
					Genre="Hands Up",
					ExtendedMix=true,
					RadioMix=false,
					Played=true,
					PlayedInEp=18,
					Stored=true,
					Wanted=false
				}
				);
		}
	}
}
