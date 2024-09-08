using Microsoft.EntityFrameworkCore;
using RMD.Data.Context;
using RMD.Data.Models;
using System.Reflection.Metadata;

namespace RMD.Business.Services
{
	// Class contract Interfaces - add more as needed
	public interface IArtistService
	{
		Task<Result<IEnumerable<Artist>>> GetAllArtistsAsync();
		Task<Result<Artist>> GetArtistByNameAsync(string artistName);
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
				return Result<IEnumerable<Artist>>.Failure("An unknown error occured while fetching all artists from the database." + ex.Message);
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
				return Result<Artist>.Failure("An unknown error occured while fetching a single artist from the database." + ex.Message);
			}
			

			
		}
	}
}
