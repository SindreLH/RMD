using System.ComponentModel.DataAnnotations;

namespace RMD.Data.Models.DTO
{
	public class SongDto
	{
		public int SongId { get; set; }

		[Required(ErrorMessage = "A song title is required.")]
		[StringLength(100, ErrorMessage = "The song title cannot exceed 100 characters.")]
		public required string Title { get; set; }

		public string? RemixArtist { get; set; }


		//OLD STRING BASED SETUP - REMOVE:

		//[Required(ErrorMessage = "An artist is required.")]
		//public required string Artist { get; set; }

		[Required(ErrorMessage = "Song length is required.")]
		public required string Length { get; set; }

		[Required(ErrorMessage = "Song genre is required.")]
		public required string Genre { get; set; }


		public bool ExtendedMix { get; set; }
		public bool RadioMix { get; set; }
		public bool Played { get; set; }
		public int? PlayedInEp { get; set; }
		public bool Stored { get; set; }
		public bool Wanted { get; set; }
		public string? WantedSongUrl { get; set; }
		public bool Favorite { get; set; }



		// OLD ONE TO MANY RELATIONSHIP - REMOVE:

		//[Required(ErrorMessage = "Artist must be selected.")]
		//public int? ArtistId { get; set; }


		// NEW - MANY-TO-MANY RELATIONSHIP
		[Required(ErrorMessage = "At least one artist must be selected.")]
		public List<int> ArtistIds { get; set; } = new();
		public List<int> RemixArtistIds { get; set; } = new();
	}
}
