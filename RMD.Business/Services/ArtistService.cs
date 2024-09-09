using Microsoft.EntityFrameworkCore;
using RMD.Data.Context;
using RMD.Data.Models;
using RMD.Data.Models.DTO;
using System.Reflection.Metadata;

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
	}

	public class ArtistService : IArtistService
	{

		// Injecting the RMD Database Context into the class constructor
		private readonly RMDContext _context;
		public ArtistService(RMDContext context)
		{
			_context = context;
		}

		// Establishing methods for db interaction - add more as needed.
		// All return values wrapped in result class.

		public async Task<Result<IEnumerable<Artist>>> GetAllArtistsAsync()
		{
			try
			{
				var artists = await _context.Artists.ToListAsync();

				if (artists == null || !artists.Any())
				{
					// Consider adding logging here:
					return Result<IEnumerable<Artist>>.Failure("No artists were found in the database.");
				}

				// Consider adding logging here:
				return Result<IEnumerable<Artist>>.Success(artists);
			}

			catch (Exception ex)
			{
				// Consider adding logging here:
				return Result<IEnumerable<Artist>>.Failure("An unknown error occured while FETCHING ALL artists from the database." + ex.Message);
			}
		}

		public async Task<Result<Artist>> GetArtistByNameAsync(string artistName)
		{

			try
			{
				var artist = await _context.Artists.Where(x => x.Name.Equals(artistName)).FirstOrDefaultAsync();

				if (artist == null)
				{
					return Result<Artist>.Failure($"The artist {artistName} does not exist in the database.");
				}

				return Result<Artist>.Success(artist);
			}

			catch (Exception ex)
			{
				return Result<Artist>.Failure("An unknown error occured while FETCHING a single artist from the database." + ex.Message);
			}
		}

		public async Task<Result<bool>> DeleteArtistByIdAsync(int artistId)
		{
			try
			{
				var artist = await _context.Artists.FindAsync(artistId);

				if (artist == null)
				{
					return Result<bool>.Failure($"Deletion failed. No artist with the ID {artistId} exists.");
				}

				_context.Artists.Remove(artist);
				await _context.SaveChangesAsync();

				return Result<bool>.Success(true);
			}

			catch (Exception ex)
			{
				// Consider adding logging here:
				return Result<bool>.Failure("An unknown error occured when deleting an artist from the database." + ex.Message);

			}
		}

		public async Task<Result<Artist>> UpdateArtistByIdAsync(int artistId, ArtistDto updatedArtistDto)
		{
			try
			{
				var artist = await _context.Artists.FindAsync(artistId);

				if (artist == null)
				{
					return Result<Artist>.Failure($"Update failed. The artist ID {artistId} does not exist in the database.");
				}

				artist.Name = updatedArtistDto.Name;
				artist.Nationality = updatedArtistDto.Nationality;
				artist.FacebookUrl = updatedArtistDto.FacebookUrl;
				artist.SoundcloudUrl = updatedArtistDto.SoundcloudUrl;
				artist.ProfilePicUrl = updatedArtistDto.ProfilePicUrl;

				_context.Artists.Update(artist);
				await _context.SaveChangesAsync();

				return Result<Artist>.Success(artist);
			}

			catch (Exception ex)
			{
				return Result<Artist>.Failure("An unknown error occured while UPDATING a single artist from the database." + ex.Message);
			}


		}

		public async Task<Result<Artist>> CreateNewArtistAsync(ArtistDto newArtistDto)
		{
			try
			{

				// Converting DTO to Artist Entity (Because: User should not be able to set ID)
				var newArtist = new Artist
				{
					Name = newArtistDto.Name,
					Nationality = newArtistDto.Nationality,
					FacebookUrl = newArtistDto.FacebookUrl,
					SoundcloudUrl = newArtistDto.SoundcloudUrl,
					ProfilePicUrl = newArtistDto.ProfilePicUrl,
				};

				await _context.Artists.AddAsync(newArtist);
				await _context.SaveChangesAsync();
				return Result<Artist>.Success(newArtist);
			}
			catch (Exception ex)
			{
				return Result<Artist>.Failure("An unknown error occured while CREATING a new artist." + ex.Message);
			}

		}
	}
}
