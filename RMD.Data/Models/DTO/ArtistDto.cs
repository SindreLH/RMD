using System.ComponentModel.DataAnnotations;

namespace RMD.Data.Models.DTO
{
	public class ArtistDto
	{
		[Required(ErrorMessage = "An artist name is required.")]
		[StringLength(100, ErrorMessage = "An artist name cannot exceed 100 characters.")]
		public required string Name { get; set; }

		[Required(ErrorMessage = "Artist nationality is required.")]
		public required string Nationality { get; set; }

		public string? FacebookUrl { get; set; }


		public string? SoundcloudUrl { get; set; }


		public string? ProfilePicUrl { get; set; }


		public string? DiscogsUrl { get; set; }

		public int ArtistId { get; set; }
	}
}
