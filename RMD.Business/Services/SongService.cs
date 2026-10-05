using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RMD.Data.Context;
using RMD.Data.Models;
using RMD.Data.Models.DTO;
using System.Text.RegularExpressions;

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
		public const int MaxTitleLength = 150;
		private static readonly Regex LengthFormat = new(@"^[0-5]?\d:[0-5]\d$");

		// Every method opens its own short-lived context (see ArtistService for why).
		private readonly IDbContextFactory<RMDContext> _contextFactory;
		private readonly ILogger<SongService> _logger;

		public SongService(IDbContextFactory<RMDContext> contextFactory, ILogger<SongService> logger)
		{
			_contextFactory = contextFactory;
			_logger = logger;
		}

		public async Task<Result<Song>> CreateNewSongAsync(SongDto newSongDto)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();

				var validation = await NormalizeAndValidateAsync(db, newSongDto, existingSongId: null);
				if (validation != null)
					return Result<Song>.Failure(validation);

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

				foreach (var id in newSongDto.ArtistIds)
					newSong.SongArtists.Add(new SongArtist { ArtistId = id, Role = ArtistRole.Primary });

				foreach (var id in newSongDto.RemixArtistIds)
					newSong.SongArtists.Add(new SongArtist { ArtistId = id, Role = ArtistRole.Remix });

				db.Songs.Add(newSong);
				await db.SaveChangesAsync();

				return Result<Song>.Success(newSong);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create song");
				return Result<Song>.Failure("An unknown error occured while CREATING a new song.");
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
				await using var db = await _contextFactory.CreateDbContextAsync();

				// Filtering happens in SQL; artists are included so the list can show them
				var query = SongsWithArtists(db);

				if (extendedMix.HasValue) query = query.Where(x => x.ExtendedMix == extendedMix.Value);
				if (radioMix.HasValue) query = query.Where(x => x.RadioMix == radioMix.Value);
				if (favorite.HasValue) query = query.Where(x => x.Favorite == favorite.Value);
				if (played.HasValue) query = query.Where(x => x.Played == played.Value);
				if (stored.HasValue) query = query.Where(x => x.Stored == stored.Value);
				if (wanted.HasValue) query = query.Where(x => x.Wanted == wanted.Value);

				var songs = await query.ToListAsync();

				if (!songs.Any())
				{
					return Result<IEnumerable<Song>>.Failure("No songs were found in the database.");
				}

				return Result<IEnumerable<Song>>.Success(songs);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch filtered songs");
				return Result<IEnumerable<Song>>.Failure("An unknown error occured while FETCHING ALL songs from the database.");
			}
		}

		//FOR DELETE SONG MODAL - REFECTH SELECTED SONG WITH MANY-TO-MANY RELATIONSHIP
		public async Task<Result<Song>> GetSongByIdWithArtistsAsync(int songId)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var song = await SongsWithArtists(db).FirstOrDefaultAsync(s => s.SongId == songId);

				if (song == null)
					return Result<Song>.Failure("Låten ble ikke funnet.");

				return Result<Song>.Success(song);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch song {SongId}", songId);
				return Result<Song>.Failure("En ukjent feil oppstod ved henting av låten.");
			}
		}

		public async Task<Result<IEnumerable<Song>>> GetAllSongsAsync()
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var songs = await SongsWithArtists(db).ToListAsync();

				if (!songs.Any())
				{
					return Result<IEnumerable<Song>>.Failure("No songs were found in the database.");
				}

				return Result<IEnumerable<Song>>.Success(songs);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch songs");
				return Result<IEnumerable<Song>>.Failure("An unknown error occured while FETCHING ALL songs from the database.");
			}
		}

		public async Task<Result<IEnumerable<Song>>> GetSongsByArtistIdAsync(int artistId)
		{

			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var songs = await SongsWithArtists(db)
					.Where(s => s.SongArtists.Any(sa => sa.ArtistId == artistId))
					.ToListAsync();

				if (!songs.Any())
				{
					return Result<IEnumerable<Song>>.Failure($"The artist with ID: {artistId} does not hold any songs. Register a song and attach this artist.");
				}

				return Result<IEnumerable<Song>>.Success(songs);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch songs for artist {ArtistId}", artistId);
				return Result<IEnumerable<Song>>.Failure("An unknown error occured while FETCHING the artist's songs from the database.");
			}

		}

		public async Task<Result<Song>> GetSongByTitleAsync(string songTitle)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var title = songTitle.Trim();
				var song = await db.Songs.AsNoTracking().FirstOrDefaultAsync(x => x.Title == title);

				if (song == null)
				{
					return Result<Song>.Failure($"The song {songTitle} does not exist in the database.");
				}

				return Result<Song>.Success(song);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch song by title");
				return Result<Song>.Failure("An unknown error occured while FETCHING a single song from the database.");
			}
		}

		public async Task<Result<bool>> DeleteSongByIdAsync(int songId)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var song = await db.Songs.FindAsync(songId);

				if (song == null)
				{
					return Result<bool>.Failure($"Deletion failed. No song with ID {songId} exists in the database.");
				}

				db.Songs.Remove(song);
				await db.SaveChangesAsync();

				return Result<bool>.Success(true);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete song {SongId}", songId);
				return Result<bool>.Failure("An unknown error occured when deleting a song from the database.");
			}
		}

		public async Task<Result<Song>> UpdateSongByIdAsync(int songId, SongDto updatedSongDto)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var song = await db.Songs
					.Include(s => s.SongArtists)
					.FirstOrDefaultAsync(s => s.SongId == songId);

				if (song == null)
					return Result<Song>.Failure("Song not found.");

				var validation = await NormalizeAndValidateAsync(db, updatedSongDto, existingSongId: songId);
				if (validation != null)
					return Result<Song>.Failure(validation);

				// Replace only the links that changed (the key is SongId + ArtistId + Role)
				var wanted = updatedSongDto.ArtistIds.Select(id => (id, ArtistRole.Primary))
					.Concat(updatedSongDto.RemixArtistIds.Select(id => (id, ArtistRole.Remix)))
					.ToHashSet();

				foreach (var link in song.SongArtists.Where(sa => !wanted.Contains((sa.ArtistId, sa.Role))).ToList())
					song.SongArtists.Remove(link);

				foreach (var (artistId, role) in wanted)
				{
					if (!song.SongArtists.Any(sa => sa.ArtistId == artistId && sa.Role == role))
						song.SongArtists.Add(new SongArtist { SongId = songId, ArtistId = artistId, Role = role });
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

				await db.SaveChangesAsync();

				return Result<Song>.Success(song);
			}

			catch(Exception ex)
			{
				_logger.LogError(ex, "Failed to update song {SongId}", songId);
				return Result<Song>.Failure("An unknown error occured while UPDATING a single song in the the database.");
			}
		}

		private static IQueryable<Song> SongsWithArtists(RMDContext db) =>
			db.Songs
				.AsNoTracking()
				.Include(s => s.SongArtists)
					.ThenInclude(sa => sa.Artist);

		/// <summary>
		/// Trims and cleans the dto in place, then checks it. Returns an error message, or null when valid.
		/// </summary>
		private static async Task<string?> NormalizeAndValidateAsync(RMDContext db, SongDto dto, int? existingSongId)
		{
			dto.Title = string.Join(' ', (dto.Title ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
			dto.Genre = (dto.Genre ?? "").Trim();
			dto.Length = (dto.Length ?? "").Trim();
			dto.ArtistIds = (dto.ArtistIds ?? new()).Distinct().ToList();
			// An artist can't be both the original artist and the remixer of the same song
			dto.RemixArtistIds = (dto.RemixArtistIds ?? new()).Distinct().Except(dto.ArtistIds).ToList();
			dto.PlayedInEp = dto.Played ? dto.PlayedInEp : null;
			dto.WantedSongUrl = dto.Wanted ? SafeUrl.Normalize(dto.WantedSongUrl) : null;

			if (dto.Title.Length == 0) return "A song title is required.";
			if (dto.Title.Length > MaxTitleLength) return $"The song title cannot exceed {MaxTitleLength} characters.";
			if (dto.Genre.Length == 0) return "Song genre is required.";
			if (!LengthFormat.IsMatch(dto.Length)) return "Length input must match format: MM:SS";
			if (dto.PlayedInEp < 0) return "Episode number cannot be negative.";
			if (dto.ArtistIds.Count == 0) return "No valid artists selected.";

			var allIds = dto.ArtistIds.Concat(dto.RemixArtistIds).ToList();
			var existingIds = await db.Artists.Where(a => allIds.Contains(a.ArtistId)).CountAsync();
			if (existingIds != allIds.Count) return "One or more selected artists no longer exist.";

			if (await IsDuplicateAsync(db, dto, existingSongId))
				return $"A song with the name {dto.Title} by this artist already exists in the database.";

			return null;
		}

		/// <summary>
		/// A song is a duplicate when title (case-insensitive), primary artists, remix artists
		/// and the Extended/Radio flags all match. Remixes and other artists' songs with the same title are allowed.
		/// </summary>
		private static async Task<bool> IsDuplicateAsync(RMDContext db, SongDto dto, int? existingSongId)
		{
			var candidates = await db.Songs
				.Where(s => s.Title == dto.Title
					&& s.ExtendedMix == dto.ExtendedMix
					&& s.RadioMix == dto.RadioMix
					&& s.SongId != (existingSongId ?? 0))
				.Select(s => s.SongArtists.Select(sa => new { sa.ArtistId, sa.Role }).ToList())
				.ToListAsync();

			var primary = dto.ArtistIds.ToHashSet();
			var remix = dto.RemixArtistIds.ToHashSet();

			return candidates.Any(links =>
				links.Where(l => l.Role == ArtistRole.Primary).Select(l => l.ArtistId).ToHashSet().SetEquals(primary) &&
				links.Where(l => l.Role == ArtistRole.Remix).Select(l => l.ArtistId).ToHashSet().SetEquals(remix));
		}
	}
}
