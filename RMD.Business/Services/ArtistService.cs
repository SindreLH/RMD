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
		Task<Result<List<Artist>>> GetAliasesAsync(int artistId);
		Task<Result<List<Artist>>> SetAliasesAsync(int artistId, IEnumerable<int> aliasIds);
	}

	public class ArtistService : IArtistService
	{
		/// <summary>An artist can have at most this many aliases (other artists in the same alias group).</summary>
		public const int MaxAliases = 5;

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

		public async Task<Result<List<Artist>>> GetAliasesAsync(int artistId)
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var group = await db.Artists.Where(a => a.ArtistId == artistId).Select(a => a.AliasGroupId).FirstOrDefaultAsync();

				var aliases = group == null
					? new List<Artist>()
					: await db.Artists.AsNoTracking()
						.Where(a => a.AliasGroupId == group && a.ArtistId != artistId)
						.OrderBy(a => a.Name)
						.ToListAsync();

				return Result<List<Artist>>.Success(aliases);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch aliases for artist {ArtistId}", artistId);
				return Result<List<Artist>>.Failure("An unknown error occured while fetching the artist's aliases.");
			}
		}

		/// <summary>
		/// Makes exactly <paramref name="aliasIds"/> the aliases of the artist. Aliases are mutual: the artist and its
		/// aliases share one alias group. An artist picked from another group moves into this one; artists that are no
		/// longer chosen leave it. Groups left with a single artist are dissolved.
		/// </summary>
		public async Task<Result<List<Artist>>> SetAliasesAsync(int artistId, IEnumerable<int> aliasIds)
		{
			var ids = aliasIds.Where(id => id != artistId).Distinct().ToList();
			if (ids.Count > MaxAliases)
				return Result<List<Artist>>.Failure($"An artist can have at most {MaxAliases} aliases.");

			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				await using var transaction = await db.Database.BeginTransactionAsync();

				var artist = await db.Artists.FindAsync(artistId);
				if (artist == null)
					return Result<List<Artist>>.Failure($"No artist with ID: {artistId} exists in the database.");

				var chosen = await db.Artists.Where(a => ids.Contains(a.ArtistId)).ToListAsync();
				if (chosen.Count != ids.Count)
					return Result<List<Artist>>.Failure("One or more selected aliases no longer exist.");

				var currentGroup = artist.AliasGroupId;
				var touchedGroups = chosen.Select(a => a.AliasGroupId).Append(currentGroup)
					.Where(g => g != null).Select(g => g!.Value).Distinct().ToList();

				// Members of the current group that were not chosen leave it
				if (currentGroup != null)
				{
					var leaving = await db.Artists
						.Where(a => a.AliasGroupId == currentGroup && a.ArtistId != artistId && !ids.Contains(a.ArtistId))
						.ToListAsync();
					foreach (var a in leaving)
						a.AliasGroupId = null;
				}

				var group = chosen.Count == 0 ? (Guid?)null : currentGroup ?? Guid.NewGuid();
				artist.AliasGroupId = group;
				foreach (var a in chosen)
					a.AliasGroupId = group;

				await db.SaveChangesAsync();
				await DissolveSingleMemberGroupsAsync(db, touchedGroups);
				await transaction.CommitAsync();

				var aliases = chosen.OrderBy(a => a.Name).ToList();
				return Result<List<Artist>>.Success(aliases);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to set aliases for artist {ArtistId}", artistId);
				return Result<List<Artist>>.Failure("An unknown error occured while saving the artist's aliases.");
			}
		}

		/// <summary>An alias group of one is not an alias any more.</summary>
		private static async Task DissolveSingleMemberGroupsAsync(RMDContext db, IEnumerable<Guid> groups)
		{
			foreach (var group in groups.Distinct())
			{
				var members = await db.Artists.Where(a => a.AliasGroupId == group).ToListAsync();
				if (members.Count == 1)
				{
					members[0].AliasGroupId = null;
					await db.SaveChangesAsync();
				}
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

				var aliasGroup = artist.AliasGroupId;
				db.Artists.Remove(artist);
				await db.SaveChangesAsync();

				if (aliasGroup != null)
					await DissolveSingleMemberGroupsAsync(db, new[] { aliasGroup.Value });

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
