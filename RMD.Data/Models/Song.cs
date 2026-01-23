using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMD.Data.Models
{
	public class Song
	{
        [Key]
        public int SongId { get; set; }

        public DateTime SongCreatedAt { get; set; } = DateTime.UtcNow;


        [Required]
        [MaxLength(150)]
        public required string Title { get; set; }

        public string? RemixArtist { get; set; }

		//[Required]
  //      public required string Artist { get; set; }
		
		[Required]
        public required string Length { get; set; }

		[Required]
        public required string Genre { get; set; }
        public bool ExtendedMix { get; set; }
        public bool RadioMix { get; set; }
        public bool Played { get; set; }
        public int? PlayedInEp { get; set; }
        public bool Stored { get; set; }
        public bool Wanted { get; set; }
        public string? WantedSongUrl { get; set; }
        public bool Favorite { get; set; }

        //OLD SETUP - ONE TO MANY RELATIONSHIP BETWEEN ARTIST AND SONG
		//public int ArtistId { get; set; }

  //      [Required]
  //      public Artist Artist { get; set; } = null!;

        //NEW SETUP - MANY-TO-MANY WHERE A SONG CAN HAVE MULTIPLE ARTISTS
        public ICollection<Artist> Artists { get; set; } = new List<Artist>();
		public ICollection<SongArtist> SongArtists { get; set; } = new List<SongArtist>();


		public Song()
        {
            
        }

    }
}
