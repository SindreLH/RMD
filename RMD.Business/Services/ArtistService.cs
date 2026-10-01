using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RMD.Data.Context;
using RMD.Data.Models;
using RMD.Data.Models.DTO;


namespace RMD.Business.Services
{
	// Class contract Interfaces - add more as needed
	public interface IArtistService
	{
		Task<Result<IEnumerable<Artist>>> GetAllArtistsAsync();
		Task<Result<Artist>> GetArtistByNameAsync(string artistName);
		Task<Result<bool>> DeleteArtistByIdAsync(int artistId);
		Task<Result<Artist>> UpdateArtistByIdAsync(int artistId, ArtistDto updatedArtistDto);
		Task<Result<Artist>> CreateNewArtistAsync(ArtistDto newArtist);
		Task<Result<Artist>> GetArtistById(int artistId);
		Task<Result<int>> CountSongsLeftWithoutArtistAsync(int artistId);
	}

	public class ArtistService : IArtistService
	{

		// Every method opens its own short-lived context. In Blazor Server a scoped DbContext would live
		// for the whole browser tab, so overlapping UI events would share it and failed saves would linger.
		private readonly IDbContextFactory<RMDContext> _contextFactory;
		private readonly ILogger<ArtistService> _logger;

		public ArtistService(IDbContextFactory<RMDContext> contextFactory, ILogger<ArtistService> logger)
		{
			_contextFactory = contextFactory;
			_logger = logger;
		}

		// Establishing methods for db interaction - add more as needed.
		// All return values wrapped in result class.

		public async Task<Result<IEnumerable<Artist>>> GetAllArtistsAsync()
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var artists = await db.Artists.AsNoTracking().ToListAsync();

				if (!artists.Any())
				{
					return Result<IEnumerable<Artist>>.Failure("No artists were found in the database.");
				}

				return Result<IEnumerable<Artist>>.Success(artists);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch artists");
				return Result<IEnumerable<Artist>>.Failure("An unknown error occured while FETCHING ALL artists from the database.");
			}
		}

		public async Task<Result<Artist>> GetArtistByNameAsync(string artistName)
		{

			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var name = artistName.Trim();
				var artist = await db.Artists.AsNoTracking().FirstOrDefaultAsync(x => x.Name == name);

				if (artist == null)
				{
					return Result<Artist>.Failure($"The artist {artistName} does not exist in the database.");
				}

				return Result<Artist>.Success(artist);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch artist by name");
				return Result<Artist>.Failure("An unknown error occured while FETCHING a single artist from the database.");
			}
		}

		public async Task<Result<Artist>> GetArtistById(int artistId)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var artist = await db.Artists.AsNoTracking().FirstOrDefaultAsync(x => x.ArtistId == artistId);

				if(artist == null)
				{
					return Result<Artist>.Failure($"No artist with ID: {artistId} exists in the database.");
				}

				return Result<Artist>.Success(artist);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch artist {ArtistId}", artistId);
				return Result<Artist>.Failure("An unknown error occured while FETCHING a single artist from the database.");
			}
		}

		/// <summary>
		/// Number of songs that would have no primary artist left if this artist were deleted
		/// (shown as a warning before deleting).
		/// </summary>
		public async Task<Result<int>> CountSongsLeftWithoutArtistAsync(int artistId)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var count = await db.Songs.CountAsync(s =>
					s.SongArtists.Any(sa => sa.ArtistId == artistId) &&
					!s.SongArtists.Any(sa => sa.ArtistId != artistId && sa.Role == ArtistRole.Primary));

				return Result<int>.Success(count);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to count songs for artist {ArtistId}", artistId);
				return Result<int>.Failure("An unknown error occured while counting the artist's songs.");
			}
		}

		public async Task<Result<bool>> DeleteArtistByIdAsync(int artistId)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var artist = await db.Artists.FindAsync(artistId);

				if (artist == null)
				{
					return Result<bool>.Failure($"Deletion failed. No artist with the ID {artistId} exists in the database.");
				}

				db.Artists.Remove(artist);
				await db.SaveChangesAsync();

				return Result<bool>.Success(true);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete artist {ArtistId}", artistId);
				return Result<bool>.Failure("An unknown error occured when deleting an artist from the database.");

			}
		}

		public async Task<Result<Artist>> UpdateArtistByIdAsync(int artistId, ArtistDto updatedArtistDto)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var artist = await db.Artists.FindAsync(artistId);

				if (artist == null)
				{
					return Result<Artist>.Failure($"Update failed. The artist ID {artistId} does not exist in the database.");
				}

				var name = updatedArtistDto.Name.Trim();
				if (string.IsNullOrEmpty(name))
				{
					return Result<Artist>.Failure("An artist name is required.");
				}

				if (await db.Artists.AnyAsync(a => a.Name == name && a.ArtistId != artistId))
				{
					return Result<Artist>.Failure($"An artist with the name {name} already exists in the database.");
				}

				artist.Name = name;
				artist.Nationality = updatedArtistDto.Nationality.Trim();
				artist.FacebookUrl = SafeUrl.Normalize(updatedArtistDto.FacebookUrl);
				artist.SoundcloudUrl = SafeUrl.Normalize(updatedArtistDto.SoundcloudUrl);
				artist.DiscogsUrl = SafeUrl.Normalize(updatedArtistDto.DiscogsUrl);
				artist.ProfilePicUrl = SafeUrl.Normalize(updatedArtistDto.ProfilePicUrl);

				await db.SaveChangesAsync();

				return Result<Artist>.Success(artist);
			}

			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to update artist {ArtistId}", artistId);
				return Result<Artist>.Failure("An unknown error occured while UPDATING a single artist in the database.");
			}


		}

		public async Task<Result<Artist>> CreateNewArtistAsync(ArtistDto newArtistDto)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();

				var name = newArtistDto.Name.Trim();
				if (string.IsNullOrEmpty(name))
				{
					return Result<Artist>.Failure("An artist name is required.");
				}

				if (await db.Artists.AnyAsync(x => x.Name == name))
				{
					return Result<Artist>.Failure($"An artist with the name {name} already exists in the database.");
				}

				// Explicitly converting DTO to Artist Entity (Because: User should not be able to set ID)
				var newArtist = new Artist
				{
					Name = name,
					Nationality = newArtistDto.Nationality.Trim(),
					FacebookUrl = SafeUrl.Normalize(newArtistDto.FacebookUrl),
					SoundcloudUrl = SafeUrl.Normalize(newArtistDto.SoundcloudUrl),
					ProfilePicUrl = SafeUrl.Normalize(newArtistDto.ProfilePicUrl),
					DiscogsUrl = SafeUrl.Normalize(newArtistDto.DiscogsUrl)
				};

				db.Artists.Add(newArtist);
				await db.SaveChangesAsync();
				return Result<Artist>.Success(newArtist);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create artist");
				return Result<Artist>.Failure("An unknown error occured while CREATING a new artist.");
			}

		}
	}
}
