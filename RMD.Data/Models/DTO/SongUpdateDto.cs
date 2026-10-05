using System.ComponentModel.DataAnnotations;

namespace RMD.Data.Models.DTO
{
	public class SongUpdateDto
	{
		public int SongId { get; set; }

		[Required(ErrorMessage = "Tittel må fylles ut.")]
		[StringLength(150, ErrorMessage = "Tittelen kan ikke være lengre enn 150 tegn.")]
		public string Title { get; set; } = string.Empty;

		[Required(ErrorMessage = "Lengde må fylles ut.")]
		[RegularExpression(@"^[0-5]?\d:[0-5]\d$",
		ErrorMessage = "Lengde må ha formatet MM:SS")]
		public string Length { get; set; } = string.Empty;

		[Required(ErrorMessage = "Velg en sjanger.")]
		public string Genre { get; set; } = string.Empty;
		public bool ExtendedMix { get; set; }
		public bool RadioMix { get; set; }
		public bool Favorite { get; set; }
		public bool Played { get; set; }

		[Range(0, 9999, ErrorMessage = "Ugyldig episodenummer.")]
		public int? PlayedInEp { get; set; }
		public bool Stored { get; set; }
		public bool Wanted { get; set; }
		public string? WantedSongUrl { get; set; } = string.Empty;

		public List<int> ArtistIds { get; set; } = new();
		public List<int> RemixArtistIds { get; set; } = new();
	}
}
