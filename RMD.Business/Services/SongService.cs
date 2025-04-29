using Microsoft.EntityFrameworkCore;
using RMD.Data.Context;
using RMD.Data.Models;
using RMD.Data.Models.DTO;
using System.Linq.Expressions;
using System.Numerics;

namespace RMD.Business.Services
{
	public interface ISongService
	{
		Task<Result<Song>> CreateNewSongAsync(SongDto newSongDto);
		Task<Result<Song>> GetSongByTitleAsync(string songTitle);

		Task<Result<IEnumerable<Song>>> GetAllSongsAsync();
		Task<Result<IEnumerable<Song>>> GetAllSongsWithFiltersAsync(
			bool? ExtendedMix,
			bool? RadioMix,
			bool? Favorite,
			bool? Played,
			bool? Stored,
			bool? Wanted);

		Task<Result<bool>> DeleteSongByIdAsync(int songId);
		Task<Result<Song>> UpdateSongByIdAsync(int songId, SongDto updatedSongDto);
		Task<Result<ICollection<Song>>> GetSongsByArtistIdAsync(int artistId);

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

				var existingSong = await _context.Songs.
					Where(x => x.Title == newSongDto.Title).FirstOrDefaultAsync();

				if (existingSong != null)
				{
					return Result<Song>.Failure($"A song with the name {newSongDto.Title} already exists in the database.");
				}


				var artist = await _context.Artists.FindAsync(newSongDto.ArtistId);
				if(artist == null)
				{
					return Result<Song>.Failure("Artist not found.");
				}

				var newSong = new Song
				{
					Title = newSongDto.Title,
					RemixArtist = newSongDto.RemixArtist,
					Length = newSongDto.Length,
					Genre = newSongDto.Genre,
					ExtendedMix = newSongDto.ExtendedMix,
					RadioMix = newSongDto.RadioMix,
					Played = newSongDto.Played,
					PlayedInEp = newSongDto.PlayedInEp,
					Stored = newSongDto.Stored,
					Wanted = newSongDto.Wanted,
					WantedSongUrl = newSongDto.WantedSongUrl,
					Favorite = newSongDto.Favorite,

					ArtistId = newSongDto.ArtistId.Value,
					Artist = artist,

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


		public async Task<Result<IEnumerable<Song>>> GetAllSongsWithFiltersAsync(
		bool? extendedMix,
		bool? radioMix,
		bool? favorite,
		bool? played,
		bool? stored,
		bool? wanted)
		{
			try
			{
				var songs = await _context.Songs.ToListAsync();

				if (songs == null || !songs.Any())
				{
					return Result<IEnumerable<Song>>.Failure("No songs were found in the database.");
				}

				// Fluent LINQ Filtering of the list of songs based on the various boolean parameters available to the user.
				songs = songs
					.Where(x => !extendedMix.HasValue || x.ExtendedMix == extendedMix.Value)
					.Where(x => !radioMix.HasValue || x.RadioMix == radioMix.Value)
					.Where(x => !favorite.HasValue || x.Favorite == favorite.Value)
					.Where(x => !played.HasValue || x.Played == played.Value)
					.Where(x => !stored.HasValue || x.Stored == stored.Value)
					.Where(x => !wanted.HasValue || x.Wanted == wanted.Value)
					.ToList();

				return Result<IEnumerable<Song>>.Success(songs);
			}

			catch (Exception ex)
			{
				return Result<IEnumerable<Song>>.Failure("An unknown error occured while FETCHING ALL songs from the database." + ex.Message);
			}
		}

		public async Task<Result<IEnumerable<Song>>> GetAllSongsAsync()
		{
			try
			{
				var songs = await _context.Songs.ToListAsync();

				if (songs == null || !songs.Any())
				{
					return Result<IEnumerable<Song>>.Failure("No songs were found in the database.");
				}

				return Result<IEnumerable<Song>>.Success(songs);
			}

			catch (Exception ex)
			{
				return Result<IEnumerable<Song>>.Failure("An unknown error occured while FETCHING ALL songs from the database." + ex.Message);
			}
		}

		public async Task<Result<ICollection<Song>>> GetSongsByArtistIdAsync(int artistId)
		{

			try
			{
				var artist = await _context.Artists.Where(x => x.ArtistId.Equals(artistId)).FirstOrDefaultAsync();
				var songs = await _context.Songs.Where(x => x.ArtistId.Equals(artistId)).ToListAsync();

				if (artist == null)
				{
					return Result<ICollection<Song>>.Failure($"No songs could be found because no artist with ID: {artistId} exists in the database.");
				}


				if (!songs.Any())
				{
					return Result<ICollection<Song>>.Failure($"The artist with ID: {artistId} does not hold any songs. Register a song and attach this artist.");
				}


				return Result<ICollection<Song>>.Success(songs);
			}

			catch (Exception ex)
			{
				return Result<ICollection<Song>>.Failure("An unknown error occured while FETCHING a single artist from the database." + ex.Message);
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

		public async Task<Result<bool>> DeleteSongByIdAsync(int songId)
		{
			try
			{
				var song = await _context.Songs.FindAsync(songId);

				if (song == null)
				{
					return Result<bool>.Failure($"Deletion failed. No song with ID {songId} exists in the database.");
				}

				_context.Remove(song);
				await _context.SaveChangesAsync();

				return Result<bool>.Success(true);
			}

			catch (Exception ex)
			{
				return Result<bool>.Failure("An unknown error occured when deleting a song from the database." + ex.Message);
			}
		}

		public async Task<Result<Song>> UpdateSongByIdAsync(int songId, SongDto updatedSongDto)
		{
			try
			{
				var song = await _context.Songs.FindAsync(songId);

				if(song == null)
				{
					return Result<Song>.Failure("Update failed. The song ID {songId} does not exist in the database.");

				}

				song.Title = updatedSongDto.Title;
				song.RemixArtist = updatedSongDto.RemixArtist;
				song.ArtistId = updatedSongDto.ArtistId.Value;
				song.Length = updatedSongDto.Length;
				song.Genre = updatedSongDto.Genre;
				song.ExtendedMix = updatedSongDto.ExtendedMix;
				song.RadioMix = updatedSongDto.RadioMix;
				song.Played = updatedSongDto.Played;
				song.PlayedInEp = updatedSongDto.PlayedInEp;
				song.Stored = updatedSongDto.Stored;
				song.Wanted = updatedSongDto.Wanted;
				song.WantedSongUrl = updatedSongDto.WantedSongUrl;

				_context.Songs.Update(song);
				await _context.SaveChangesAsync();

				return Result<Song>.Success(song);
			}

			catch(Exception ex)
			{
				return Result<Song>.Failure("An unknown error occured while UPDATING a single song in the the database." + ex.Message);
			}
		}
	} 
}
