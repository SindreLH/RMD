using Microsoft.AspNetCore.Mvc;
using RMD.Business.Services;
using RMD.Data.Models;
using RMD.Data.Models.DTO;

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
		[HttpGet(Name = "GetAllArtists")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Artist>))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		public async Task<IActionResult> GetAllArtists()
		{

			var result = await _artistService.GetAllArtistsAsync();

			if (!result.IsSuccess)
			{
				return NotFound(result.Error);
			}

			return Ok(result.Value);
		}

		/// <summary>
		/// Gets a specific artist from the database using a search string.
		/// </summary>
		/// <param name="artistName">The name of an artist entity.</param>
		/// <returns>
		/// A single artist entity.
		/// </returns>
		/// <Remarks>
		/// Possible error messages include:
		/// - "The artist {artistName} does not exist in the database."
		/// - "An unknown error occured while fetching artists from the database."
		/// </Remarks>
		[HttpGet("Search/{artistName}", Name="GetSpecificArtist")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Artist))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		public async Task<IActionResult> GetArtistByName(string artistName)
		{
			var result = await _artistService.GetArtistByNameAsync(artistName);

			if (!result.IsSuccess)
			{
				return NotFound(result.Error);
			}

			return Ok(result.Value);
		}

		/// <summary>
		/// Deletes a specific artist from the database using the artist entity's ID.
		/// </summary>
		/// <param name="artistId">The ID of an artist entity.</param>
		/// <returns>
		/// A Boolean result value, indicating the operations success or failure status.
		/// </returns>
		/// <Remarks>
		/// Possible error messages include:
		/// - "Deletion failed. No artist with the ID {artistId} exists."
		/// - "An unknown error occured while fetching artists from the database."
		/// </Remarks>
		[HttpDelete("{artistId:int}", Name = "DeleteSpecificArtist")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		public async Task<IActionResult> DeleteArtistById(int artistId)
		{
			var result = await _artistService.DeleteArtistByIdAsync(artistId);

			if (!result.IsSuccess)
			{
				return NotFound(result.Error);
			}

			return Ok(result.Value);
		}

		/// <summary>
		/// Updates a specific artist in the database using the artist entity's ID.
		/// </summary>
		/// <param name="artistId">The ID of an artist entity.</param>
		/// <param name="updatedArtistDto">The updated version of the selected artist entity. Minus the ID field.</param>
		/// <returns>
		/// Returns an updated version of the selected artist entity.
		/// </returns>
		/// <Remarks>
		/// Possible error messages include:
		/// - "Update failed. The artist ID {artistId} does not exist in the database."
		/// - "An unknown error occured while fetching artists from the database."
		/// </Remarks>
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Artist))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		[HttpPatch("{artistId:int}", Name = "UpdateSpecificArtist")]
		public async Task<IActionResult> UpdateArtistById(int artistId, [FromBody] ArtistDto updatedArtistDto)
		{
			var result = await _artistService.UpdateArtistByIdAsync(artistId, updatedArtistDto);

			if (!result.IsSuccess)
			{
				return NotFound(result.Error);
			}

			return Ok(result.Value);
		}

		/// <summary>
		/// Creates a new artist entity in the database.
		/// </summary>
		/// <param name="newArtistDto">DTO of a new artist entity minus ID field.</param>
		/// <returns>
		/// Returns the newly created artist entity.
		/// </returns>
		/// <Remarks>
		/// Possible error messages include:
		/// - "Update failed. The artist ID {artistId} does not exist in the database."
		/// - "An unknown error occured while fetching artists from the database."
		/// </Remarks>
		[ProducesResponseType(StatusCodes.Status201Created, Type=typeof(Artist))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type=typeof(string))]
		[HttpPost(Name = "CreateArtist")]
		public async Task<IActionResult> CreateArtist(ArtistDto newArtistDto)
		{
			var result = await _artistService.CreateNewArtistAsync(newArtistDto);

			if (!result.IsSuccess)
			{
				return BadRequest(result.Error);
			}

			return Created();
		}
	}
}
