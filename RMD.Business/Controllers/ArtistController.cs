﻿using Microsoft.AspNetCore.Mvc;
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
		/// Gets a specific artist from the database based on a search string.
		/// </summary>
		/// <returns>
		/// A single artist entity.
		/// </returns>
		/// <Remarks>
		/// Possible error messages include:
		/// - "The artist {artistName} does not exist in the database."
		/// - "An unknown error occured while fetching artists from the database."
		/// </Remarks>
		[HttpGet("search", Name = "GetSpecificArtist")]
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
	}
}
