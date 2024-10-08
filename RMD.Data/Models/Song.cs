﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RMD.Data.Models
{
	public class Song
	{
        [Key]
        public int SongId { get; set; }

        [Required]
        [MaxLength(150)]
        public required string Title { get; set; }

        public string? RemixArtist { get; set; }

		[Required]
        public required string Artist { get; set; }
		//public ICollection<Artist> Artist { get; set; } = new List<Artist>();
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

		public Song()
        {
            
        }

    }
}
