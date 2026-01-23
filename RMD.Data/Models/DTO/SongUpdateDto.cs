using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMD.Data.Models.DTO
{
	public class SongUpdateDto
	{
		public int SongId { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Length { get; set; } = string.Empty;
		public string Genre { get; set; } = string.Empty;
		public bool ExtendedMix { get; set; }
		public bool Favorite { get; set; }
		public bool Played { get; set; }
		public int? PlayedInEp { get; set; }
		public bool Stored { get; set; }
		public bool Wanted { get; set; }
		public string? WantedSongUrl { get; set; } = string.Empty;

		public List<int> ArtistIds { get; set; } = new();   
		public List<int> RemixArtistIds { get; set; } = new();  
	}
}
