using System.ComponentModel.DataAnnotations;

namespace RMD.Data.Models.DTO
{
	public class SongDto
	{
		public string Title { get; set; }
		public string? RemixArtist { get; set; }
		public string Artist { get; set; }
		public string Length { get; set; }
		public string Genre { get; set; }
		public bool ExtendedMix { get; set; }
		public bool RadioMix { get; set; }
		public bool Played { get; set; }
		public int? PlayedInEp { get; set; }
		public bool Stored { get; set; }
		public bool Wanted { get; set; }
	}
}
