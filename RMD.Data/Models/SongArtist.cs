using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMD.Data.Models
{
	public class SongArtist
	{
		public int SongId { get; set; }
		public int ArtistId { get; set; }
		public ArtistRole Role { get; set; }

		public Song Song { get; set; } = null!;
		public Artist Artist { get; set; } = null!;
	}
}
