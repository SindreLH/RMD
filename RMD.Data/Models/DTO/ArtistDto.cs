using System.ComponentModel.DataAnnotations;

namespace RMD.Data.Models.DTO
{
	public class ArtistDto
	{
		[Required(ErrorMessage = "Navn må fylles ut.")]
		[StringLength(100, ErrorMessage = "Navnet kan ikke være lengre enn 100 tegn.")]
		public required string Name { get; set; }

		[Required(ErrorMessage = "Velg en nasjonalitet.")]
		public required string Nationality { get; set; }

		public string? FacebookUrl { get; set; }


		public string? SoundcloudUrl { get; set; }


		public string? ProfilePicUrl { get; set; }


		public string? DiscogsUrl { get; set; }

		public int ArtistId { get; set; }
	}
}
