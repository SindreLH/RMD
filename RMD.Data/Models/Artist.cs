using System.ComponentModel.DataAnnotations;

namespace RMD.Data.Models
{
	public class Artist
	{
        [Key]
        public int ArtistId { get; set; }

        [Required]
        public string Name { get; set; }

		[Required]
		public string Nationality { get; set; }
        public string FacebookUrl { get; set; }
        public string SoundcloudUrl { get; set; }
        public string ProfilePicUrl { get; set; }

        public Artist()
        {
            
        }

       
    }
}
