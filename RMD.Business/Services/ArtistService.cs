using Microsoft.EntityFrameworkCore;
using RMD.Data.Context;
using RMD.Data.Models;

namespace RMD.Business.Services
{
	// Class contract Interfaces - add more as needed
	public interface IArtistService
	{
		Task<Result<IEnumerable<Artist>>> GetAllArtistsAsync();
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
				return Result<IEnumerable<Artist>>.Failure("An unknown error occured while fetching artists from the database." + ex.Message);
			}
		}
	}
}
