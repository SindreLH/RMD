using RMD.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace RMD.Data.Context
{
	public class RMDContext : DbContext
	{
		public DbSet<Song> Songs { get; set; }
		public DbSet<Artist> Artists { get; set; }

		public DbSet<SongArtist> SongArtists { get; set; }


		// Constructor added for accepting DbContextOptions Configuration
		public RMDContext(DbContextOptions<RMDContext> options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			//22.01.26
			modelBuilder.Entity<SongArtist>()
				.HasKey(sa => new { sa.SongId, sa.ArtistId, sa.Role });

			modelBuilder.Entity<SongArtist>()
				.HasOne(sa => sa.Song)
				.WithMany(s => s.SongArtists)
				.HasForeignKey(sa => sa.SongId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<SongArtist>()
				.HasOne(sa => sa.Artist)
				.WithMany(a => a.SongArtists)
				.HasForeignKey(sa => sa.ArtistId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
