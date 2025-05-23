using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using RMD.Data.Context;
using RMD.Data.Models;
using RMD.Data.Models.Dashboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;



namespace RMD.Business.Services
{
	public enum Months
	{
		January = 1,
		February = 2,
		March = 3,
		April = 4,
		May = 5,
		June = 6,
		July = 7,
		August = 8,
		September = 9,
		October = 10,
		November = 11,
		December = 12
	}

	public interface IDashboardService
	{
		Task<Result<Song>> GetLatestSongAsync();
		Task<Result<Artist>> GetLatestArtistAsync();
		Task<Result<int>> GetArtistCountAsync();
		Task<Result<int>> GetSongCountAsync();
		Task<Result<int>> GetPlayedSongCountAsync();
		Task<Result<int>> GetArtistNationCountAsync();
		Task<Result<IEnumerable<Song>>> GetWantedSongsAsync();
		Task<Result<Dictionary<string, int>>> GetGenreCountAsync();
		Task<Result<List<BarChartData>>> GetMonthlyAdditionsAsync();
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
				return Result<Song>.Failure("An unknown error occured while FETCHING latest songs from the database." + ex.Message);
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
				return Result<Artist>.Failure("An unknown error occured while FETCHING LATEST artist from the database." + ex.Message);
			}
		}
		public async Task<Result<int>> GetArtistCountAsync()
		{
			try
			{
				int count = await _context.Artists.CountAsync();

				if (count == 0)
				{
					return Result<int>.Failure("No artists were found in the database.");
				}

				return Result<int>.Success(count);
			}



			catch (Exception ex)
			{
				return Result<int>.Failure("An unknown error occured while FETCHING ARTIST COUNT from the database." + ex.Message);
			}
		}
		public async Task<Result<int>> GetSongCountAsync()
		{
			try
			{
				int count = await _context.Songs.CountAsync();

				if (count == 0)
				{
					return Result<int>.Failure("No songs were found in the database.");
				}

				return Result<int>.Success(count);
			}



			catch (Exception ex)
			{
				return Result<int>.Failure("An unknown error occured while FETCHING ARTIST COUNT from the database." + ex.Message);
			}
		}
		public async Task<Result<int>> GetPlayedSongCountAsync()
		{
			try
			{
				int count = await _context.Songs
						.Where(s => s.Played)
						.CountAsync();

				if (count == 0)
				{
					return Result<int>.Failure("No PLAYED songs were found in the database.");
				}

				return Result<int>.Success(count);
			}



			catch (Exception ex)
			{
				return Result<int>.Failure("An unknown error occured while FETCHING ARTIST COUNT from the database." + ex.Message);
			}
		}
		public async Task<Result<int>> GetArtistNationCountAsync()
		{
			try
			{
				int count = await _context.Artists
					.Where(a => !string.IsNullOrEmpty(a.Nationality))
					.Select(a => a.Nationality)
					.Distinct()
					.CountAsync();

				if (count == 0)
				{
					return Result<int>.Failure("No PLAYED songs were found in the database.");
				}

				return Result<int>.Success(count);
			}



			catch (Exception ex)
			{
				return Result<int>.Failure("An unknown error occured while FETCHING ARTIST COUNT from the database." + ex.Message);
			}
		}
		public async Task<Result<IEnumerable<Song>>> GetWantedSongsAsync()
		{
			try
			{
				var songs = await _context.Songs
					.Where(s => s.Wanted)
					.ToListAsync();

				if (songs == null || !songs.Any())
				{
					return Result<IEnumerable<Song>>.Failure("No WANTED songs were found in the database.");
				}

				return Result<IEnumerable<Song>>.Success(songs);
			}

			catch (Exception ex)
			{
				return Result<IEnumerable<Song>>.Failure("An unknown error occured while FETCHING WANTED SONGS from the database." + ex.Message);
			}
		}
		public async Task<Result<Dictionary<string, int>>> GetGenreCountAsync()
		{

			try
			{
				var genreCount = await _context.Songs
					.GroupBy(s => s.Genre)
					.Select(g => new
					{
						Genre = g.Key ?? "Ukjent sjanger",
						Count = g.Count()
					})
					.ToDictionaryAsync(g => g.Genre, g => g.Count);

				return Result<Dictionary<string, int>>.Success(genreCount);
			}

			catch (Exception ex)
			{
				return Result<Dictionary<string, int>>.Failure("Error getting genre counts: " + ex.Message);
			}
		}
		public async Task<Result<List<BarChartData>>> GetMonthlyAdditionsAsync()
		{

			try
			{
				int year = DateTime.Now.Year;
				var data = new List<BarChartData>();

				foreach (Months month in Enum.GetValues(typeof(Months)))
				{
					int newSongs = await _context.Songs
						.CountAsync(s => s.SongCreatedAt.Year == year && s.SongCreatedAt.Month == (int)month);
					

					int newArtists = await _context.Artists
						.CountAsync(a => a.ArtistCreatedAt.Year == year && a.ArtistCreatedAt.Month == (int)month);

					data.Add(new BarChartData
					{
						Month = month.ToString(),
						NewSongs = newSongs,
						NewArtists = newArtists
					});
				}

				return Result<List<BarChartData>>.Success(data);
			}

			catch(Exception ex)
			{
				return Result<List<BarChartData>>.Failure("Error loading monthly data: " + ex.Message);
			}
			
		}
	}
}
