using Microsoft.AspNetCore.Mvc;
using RMD.Business.Services;
using RMD.Data.Models.DTO;

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





        [HttpPost(Name = "CreateSong")]
        public async Task<IActionResult> CreateSong(SongDto newSongDto)
        {
           var result = await _songService.CreateNewSongAsync(newSongDto);

            if (!result.IsSuccess)
            {
                return BadRequest(result.Error);
            }

            return Created();
        }

        
    }
}
