using System.ComponentModel.DataAnnotations;

namespace RMD.Data.Models.DTO
{
	public class SongDto
	{
		[Required(ErrorMessage = "A song title is required.")]
		[StringLength(100, ErrorMessage = "The song title cannot exceed 100 characters.")]
		public required string Title { get; set; }

		public string? RemixArtist { get; set; }

		[Required(ErrorMessage = "An artist is required.")]
		public required string Artist { get; set; }

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
	}
}
