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

			//29.09.25
			modelBuilder.Entity<SongArtist>()
		.HasKey(sa => new { sa.SongId, sa.ArtistId });

			modelBuilder.Entity<SongArtist>()
				.HasOne(sa => sa.Song)
				.WithMany(s => s.SongArtists)
				.HasForeignKey(sa => sa.SongId);

			modelBuilder.Entity<SongArtist>()
				.HasOne(sa => sa.Artist)
				.WithMany(a => a.SongArtists)
				.HasForeignKey(sa => sa.ArtistId);
		}
	}
}
