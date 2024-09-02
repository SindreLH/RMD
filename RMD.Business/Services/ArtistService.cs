using Microsoft.EntityFrameworkCore;
using RMD.Data.Context;
using RMD.Data.Models;

namespace RMD.Business.Services
{
	// Class contract Interfaces - add more as needed
	public interface IArtistService
	{
		Task<IEnumerable<Artist>> GetAllArtistsAsync();
	}

	public class ArtistService : IArtistService
	{

		// Injecting the RMD Database Context into the class constructor
		private readonly RMDContext _context;
		public ArtistService(RMDContext context)
		{
			_context = context;
		}

		// Establishing methods for db interaction - add more as needed
		public async Task<IEnumerable<Artist>> GetAllArtistsAsync()
		{
			return await _context.Artists.ToListAsync();
		}
	}
}
