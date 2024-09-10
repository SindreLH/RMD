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
        /// Creates a new song entity in the database.
        /// </summary>
        /// <param name="newSongDto">DTO of a new song entity minus ID field.</param>
        /// <returns>
        /// Returns status code 201 - Created.
        /// </returns>
        /// <Remarks>
        /// TBD
        /// </Remarks>
        [ProducesResponseType(StatusCodes.Status201Created, Type=typeof(Song))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type=typeof(string))]
		[HttpPost(Name = "CreateSong")]
        public async Task<IActionResult> CreateSong(SongDto newSongDto)
        {
           var result = await _songService.CreateNewSongAsync(newSongDto);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            //TODO - Implement GetSongByName and return CreatedAtAction

            // var newSong = result.Value;
            // return CreatedAtAction(nameof(GetSongByName), new { songName = newSong.Name}, newSong);
            return Created();
        }

        
    }
}
