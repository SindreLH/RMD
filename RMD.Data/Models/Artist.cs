using System.ComponentModel.DataAnnotations;

namespace RMD.Data.Models
{
	public class Artist
	{
        [Key]
        public int ArtistId { get; set; }

		public DateTime ArtistCreatedAt { get; set; } = DateTime.UtcNow;

		[Required]
        public string Name { get; set; }

        [Required]
		public string Nationality { get; set; }
        public string? FacebookUrl { get; set; }
        public string? SoundcloudUrl { get; set; }
        public string? ProfilePicUrl { get; set; }
        public string? DiscogsUrl { get; set; }

        public ICollection<Song> Songs { get; set; } = new List<Song>();

        public Artist()
        {
            Nationality = String.Empty;
            Name = String.Empty;
            FacebookUrl = String.Empty;
            SoundcloudUrl = String.Empty;
            ProfilePicUrl = String.Empty;
            DiscogsUrl = String.Empty;
        }

        public ICollection<SongArtist> SongArtists { get; set; } = new List<SongArtist>();

    }
}
