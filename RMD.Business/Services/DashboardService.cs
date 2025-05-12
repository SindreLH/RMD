using Microsoft.EntityFrameworkCore;
using RMD.Data.Context;
using RMD.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RMD.Business.Services
{
    public interface IDashboardService
    {
        Task<Result<Song>> GetLatestSongAsync();
        Task<Result<Artist>> GetLatestArtistAsync();

	}

    public class DashboardService : IDashboardService
    {
		private readonly RMDContext _context;
		public DashboardService(RMDContext context)
		{
			_context = context;
		}
	

	public async Task<Result<Song>> GetLatestSongAsync()
		{
			try
			{
				var songs = await _context.Songs
					.OrderByDescending(s => s.SongCreatedAt)
					.FirstOrDefaultAsync();

				if (songs == null)
				{
					return Result<Song>.Failure("No songs were found in the database.");
				}

				return Result<Song>.Success(songs);
			}


			catch (Exception ex)
			{
				return Result<Song>.Failure("An unknown error occured while FETCHING ALL songs from the database." + ex.Message);
			}
		}
	public async Task<Result<Artist>> GetLatestArtistAsync()
		{
			try
			{
				var artists = await _context.Artists
					.OrderByDescending(a => a.ArtistCreatedAt)
					.FirstOrDefaultAsync();

				if (artists == null)
				{
					return Result<Artist>.Failure("No songs were found in the database.");
				}

				return Result<Artist>.Success(artists);
			}


			catch (Exception ex)
			{
				return Result<Artist>.Failure("An unknown error occured while FETCHING ALL songs from the database." + ex.Message);
			}
		}

	}
}
