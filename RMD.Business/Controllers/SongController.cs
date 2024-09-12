using Microsoft.AspNetCore.Mvc;
using RMD.Business.Services;
using RMD.Data.Models.DTO;
using RMD.Data.Models;

namespace RMD.Business.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SongController : ControllerBase
    {
        
        private readonly ISongService _songService;

        public SongController(ISongService songService)
        {
            _songService = songService;
        }


        /// <summary>
        /// Gets a specific song from the database using a search string.
        /// </summary>
        /// <param name="songTitle">The title of a song entity.</param>
        /// <returns>
        /// A single song entity.
        /// </returns>
        /// <Remarks>
        /// Possible error messages include:
        /// - "The song {songTitle} does not exist in the database."
        /// - "An unknown error occured while FETCHING a single song from the database."
        /// </Remarks>

        [HttpGet("Search/{songTitle}", Name ="GetSpecificSong")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Song))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type=typeof(string))]
		public async Task<IActionResult> GetSongByTitle(string songTitle)
        {
            var result = await _songService.GetSongByTitleAsync(songTitle);

            if (!result.IsSuccess)
            {
                return NotFound(result.Error);
            }

            return Ok(result.Value);
        }


        /// <summary>
        /// Creates a new song entity and stores it in the database.
        /// </summary>
        /// <param name="newSongDto">DTO of a new song entity minus ID field.</param>
        /// <returns>
        /// Returns status code 201 - Created along with the newly created entity. 
        /// </returns>
        /// <Remarks>
        /// TBD
        /// </Remarks>
        [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(Song))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type=typeof(string))]
		[HttpPost(Name = "CreateSong")]
        public async Task<IActionResult> CreateSong(SongDto newSongDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

           var result = await _songService.CreateNewSongAsync(newSongDto);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            //TODO - Implement GetSongByName and return CreatedAtAction

            var newSong = result.Value;
            return CreatedAtAction(nameof(GetSongByTitle), new { songTitle = newSong.Title }, newSong);
        }

		/// <summary>
		/// Gets all songs from the database.
		/// </summary>
		/// <returns>
		/// A list of songs.
		/// </returns>
		/// <Remarks>
		/// Possible error messages include:
		/// - "No songs were found in the database."
		/// - "An unknown error occured while fetching artists from the database."
		/// </Remarks>
		[HttpGet(Name = "GetAllSongs")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Song>))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		public async Task<IActionResult> GetAllSongs()
		{

			var result = await _songService.GetAllSongsAsync();

			if (!result.IsSuccess)
			{
				return NotFound(result.Error);
			}

			return Ok(result.Value);
		}

		/// <summary>
		/// Deletes a specific song from the database using the song entity's ID.
		/// </summary>
		/// <param name="songId">The ID of an song entity.</param>
		/// <returns>
		/// A Boolean result value, indicating the operations success or failure status.
		/// </returns>
		/// <Remarks>
		/// Possible error messages include:
		/// - "Deletion failed. No song with the ID {songId} exists."
		/// - "An unknown error occured while fetching songs from the database."
		/// </Remarks>
		[HttpDelete("{songId:int}", Name = "DeleteSpecificSong")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		public async Task<IActionResult> DeleteSongById(int songId)
		{
			var result = await _songService.DeleteSongByIdAsync(songId);

			if (!result.IsSuccess)
			{
				return NotFound(result.Error);
			}

			return Ok(result.Value);
		}


		/// <summary>
		/// Updates a specific song in the database using the song entity's ID.
		/// </summary>
		/// <param name="songId">The ID of a song entity.</param>
		/// <param name="updatedSongDto">The updated version of the selected song entity. Minus the ID field.</param>
		/// <returns>
		/// Returns an updated version of the selected song entity.
		/// </returns>
		/// <Remarks>
		/// Possible error messages include:
		/// - "Update failed. The song ID {songId} does not exist in the database."
		/// - "An unknown error occured while fetching artists from the database."
		/// </Remarks>
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Song))]
		[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
		[HttpPatch("{songId:int}", Name = "UpdateSpecificSong")]
		public async Task<IActionResult> UpdateSongById(int songId, [FromBody] SongDto updatedSongDto)
		{
			var result = await _songService.UpdateSongByIdAsync(songId, updatedSongDto);

			if (!result.IsSuccess)
			{
				return NotFound(result.Error);
			}

			return Ok(result.Value);
		}
	}
}
