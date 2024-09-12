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

		[Url(ErrorMessage = "Please enter a valid Facebook URL.")]
		public string? FacebookUrl { get; set; }

		[Url(ErrorMessage = "Please enter a valid Soundcloud URL.")]
		public string? SoundcloudUrl { get; set; }

		[Url(ErrorMessage = "Please enter a valid URL.")]
		public string? ProfilePicUrl { get; set; }
	}
}
