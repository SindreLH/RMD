using Microsoft.AspNetCore.Mvc;
using RMD.Business.Services;
using RMD.Data.Models;

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
		/// <Remarks>
		/// Possible error messages include:
		/// - "No artists were found in the database."
		/// - "An unknown error occured while fetching artists from the database."
		/// </Remarks>
		[HttpGet(Name ="GetAllArtists")]
		[ProducesResponseType(StatusCodes.Status200OK, Type=typeof(IEnumerable<Artist>))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type=typeof(string))]
		public async Task<IActionResult> GetAllArtists()
		{

			var result = await _artistService.GetAllArtistsAsync();

			if (result == null)
			{
				return NotFound(result.Error); ;
			}

			return Ok(result.Value);
		}
	}
}
