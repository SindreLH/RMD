using Microsoft.AspNetCore.Mvc;
using RMD.Business.Services;

namespace RMD.Business.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ArtistController : ControllerBase
	{

		// Injecting ArtistService into the controllers constructor 
		private readonly IArtistService _artistService;

		public ArtistController(IArtistService artistService)
		{
			_artistService = artistService;
		}

		// Establish endpoints here

		/// <summary>
		/// Gets all artists from the database.
		/// </summary>
		/// <returns>
		/// A list of artists.
		/// </returns>
		[HttpGet(Name ="GetAllArtists")]
		public async Task<IActionResult> GetAllArtists()
		{
			var artists = await _artistService.GetAllArtistsAsync();
			return Ok(artists);
		}
	}
}
