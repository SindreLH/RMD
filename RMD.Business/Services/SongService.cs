using Microsoft.EntityFrameworkCore;
using RMD.Data.Context;
using RMD.Data.Models;
using RMD.Data.Models.DTO;

namespace RMD.Business.Services
{
	public interface ISongService
	{
		Task<Result<Song>> CreateNewSongAsync(SongDto newSongDto);
		Task<Result<Song>> GetSongByTitleAsync(string songTitle);
	}

	public class SongService : ISongService
	{
		private readonly RMDContext _context;

		public SongService(RMDContext context)
		{
			_context = context;
		}

		public async Task<Result<Song>> CreateNewSongAsync(SongDto newSongDto)
		{
			try
			{
				var newSong = new Song
				{
					Title = newSongDto.Title,
					RemixArtist = newSongDto.RemixArtist,
					Artist = newSongDto.Artist,
					Length = newSongDto.Length,
					Genre = newSongDto.Genre,
					ExtendedMix = newSongDto.ExtendedMix,
					RadioMix = newSongDto.RadioMix,
					Played = newSongDto.Played,
					PlayedInEp = newSongDto.PlayedInEp,
					Stored = newSongDto.Stored,
					Wanted = newSongDto.Wanted,
				};

				await _context.Songs.AddAsync(newSong);
				await _context.SaveChangesAsync();
				return Result<Song>.Success(newSong);
			}
			catch (Exception ex)
			{
				return Result<Song>.Failure("An unknown error occured while CREATING a new song." + ex.Message);
			}

		}

		public async Task<Result<Song>> GetSongByTitleAsync(string songTitle)
		{
			try
			{
				var song = await _context.Songs.Where(x => x.Title.Equals(songTitle)).FirstOrDefaultAsync();

				if (song == null)
				{
					return Result<Song>.Failure($"The song {songTitle} does not exist in the database.");
				}

				return Result<Song>.Success(song);
			}

			catch (Exception ex)
			{
				return Result<Song>.Failure("An unknown error occured while FETCHING a single song from the database." + ex.Message);
			}
		}
	}
}
