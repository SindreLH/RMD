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
		Task<Result<IEnumerable<Song>>> GetSongsByArtistIdAsync(int artistId);
		Task<Result<Song>> GetSongByIdWithArtistsAsync(int songId);

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
			newSongDto.RemixArtistIds ??= new();
			newSongDto.ArtistIds ??= new();

			try
			{
				//Checking dupes
				var existingSong = await _context.Songs.
					Where(x => x.Title == newSongDto.Title)
					.FirstOrDefaultAsync();

				if (existingSong != null)
				{
					return Result<Song>.Failure($"A song with the name {newSongDto.Title} by this artist already exists in the database.");
				}

				//Loading artists
				var artists = await _context.Artists
					.Where(a => newSongDto.ArtistIds.Contains(a.ArtistId))
					.ToListAsync();

				if (!artists.Any())
				{
					return Result<Song>.Failure("No valid artists selected.");
				}


				var newSong = new Song
				{
					Title = newSongDto.Title,
					Length = newSongDto.Length,
					Genre = newSongDto.Genre,
					ExtendedMix = newSongDto.ExtendedMix,
					RadioMix = newSongDto.RadioMix,
					Played = newSongDto.Played,
					PlayedInEp = newSongDto.PlayedInEp,
					Stored = newSongDto.Stored,
					Wanted = newSongDto.Wanted,
					WantedSongUrl = newSongDto.WantedSongUrl,
					Favorite = newSongDto.Favorite
				};

				foreach(var id in newSongDto.ArtistIds.Distinct())
				{
					newSong.SongArtists.Add(new SongArtist
					{
						ArtistId = id,
						Role = ArtistRole.Primary
					});
				}

				foreach (var id in newSongDto.RemixArtistIds.Distinct())
				{
					newSong.SongArtists.Add(new SongArtist
					{
						ArtistId = id,
						Role = ArtistRole.Remix
					});
				}

				_context.Songs.Add(newSong);
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

		//FOR DELETE SONG MODAL - REFECTH SELECTED SONG WITH MANY-TO-MANY RELATIONSHIP
		public async Task<Result<Song>> GetSongByIdWithArtistsAsync(int songId)
		{
			var song = await _context.Songs
				.Include(s => s.SongArtists)
					.ThenInclude(sa => sa.Artist)
				.FirstOrDefaultAsync(s => s.SongId == songId);

			if (song == null)
				return Result<Song>.Failure("Låten ble ikke funnet.");

			return Result<Song>.Success(song);
		}

		public async Task<Result<IEnumerable<Song>>> GetAllSongsAsync()
		{
			try
			{
				var songs = await _context.Songs.Include(s => s.SongArtists).ThenInclude(sa => sa.Artist).ToListAsync();

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

		public async Task<Result<IEnumerable<Song>>> GetSongsByArtistIdAsync(int artistId)
		{

			try
			{
				var songs = await _context.SongArtists
					.Where(sa => sa.ArtistId == artistId)
					.Include(sa => sa.Song)
						.ThenInclude(s => s.SongArtists)
							.ThenInclude(sa => sa.Artist)
							.Select(sa => sa.Song)
							.Distinct()
							.ToListAsync();

				if (!songs.Any())
				{
					return Result<IEnumerable<Song>>.Failure($"The artist with ID: {artistId} does not hold any songs. Register a song and attach this artist.");
				}

				return Result<IEnumerable<Song>>.Success(songs);
			}

			catch (Exception ex)
			{
				return Result<IEnumerable<Song>>.Failure("An unknown error occured while FETCHING a single artist from the database." + ex.Message);
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
				var song = await _context.Songs
					.Include(s => s.SongArtists)
					.FirstOrDefaultAsync(s => s.SongId == songId);

				if (song == null)
					return Result<Song>.Failure("Song not found.");

				_context.SongArtists.RemoveRange(song.SongArtists);
				song.SongArtists.Clear(); //Do not remove, this ensures removal of old/legacy relations

				foreach(var artistId in updatedSongDto.ArtistIds.Distinct())
				{
					song.SongArtists.Add(new SongArtist
					{
						SongId = songId,
						ArtistId = artistId,
						Role = ArtistRole.Primary
					});
				}

				foreach (var artistId in updatedSongDto.RemixArtistIds.Distinct())
				{
					song.SongArtists.Add(new SongArtist
					{
						SongId = songId,
						ArtistId = artistId,
						Role = ArtistRole.Remix
					});
				}

				song.Title = updatedSongDto.Title;
				song.Length = updatedSongDto.Length;
				song.Genre = updatedSongDto.Genre;
				song.ExtendedMix = updatedSongDto.ExtendedMix;
				song.RadioMix = updatedSongDto.RadioMix;
				song.Played = updatedSongDto.Played;
				song.PlayedInEp = updatedSongDto.PlayedInEp;
				song.Stored =updatedSongDto.Stored;	
				song.Wanted = updatedSongDto.Wanted;
				song.Favorite = updatedSongDto.Favorite;
				song.WantedSongUrl = updatedSongDto.WantedSongUrl;

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
