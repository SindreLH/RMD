using Microsoft.EntityFrameworkCore;
using RMD.Data.Context;
using RMD.Data.Models;
using RMD.Data.Models.Dashboard;



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
		// Every method opens its own short-lived context (see ArtistService for why).
		private readonly IDbContextFactory<RMDContext> _contextFactory;
		public DashboardService(IDbContextFactory<RMDContext> contextFactory)
		{
			_contextFactory = contextFactory;
		}

		public async Task<Result<Song>> GetLatestSongAsync()
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var songs = await db.Songs
					.AsNoTracking()
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
				await using var db = await _contextFactory.CreateDbContextAsync();
				var artists = await db.Artists
					.AsNoTracking()
					.OrderByDescending(a => a.ArtistCreatedAt)
					.FirstOrDefaultAsync();

				if (artists == null)
				{
					return Result<Artist>.Failure("No artists were found in the database.");
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
				await using var db = await _contextFactory.CreateDbContextAsync();
				int count = await db.Artists.CountAsync();

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
				await using var db = await _contextFactory.CreateDbContextAsync();
				int count = await db.Songs.CountAsync();

				if (count == 0)
				{
					return Result<int>.Failure("No songs were found in the database.");
				}

				return Result<int>.Success(count);
			}



			catch (Exception ex)
			{
				return Result<int>.Failure("An unknown error occured while FETCHING SONG COUNT from the database." + ex.Message);
			}
		}
		public async Task<Result<int>> GetPlayedSongCountAsync()
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				int count = await db.Songs
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
				return Result<int>.Failure("An unknown error occured while FETCHING PLAYED SONG COUNT from the database." + ex.Message);
			}
		}
		public async Task<Result<int>> GetArtistNationCountAsync()
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				int count = await db.Artists
					.Where(a => !string.IsNullOrEmpty(a.Nationality))
					.Select(a => a.Nationality)
					.Distinct()
					.CountAsync();

				if (count == 0)
				{
					return Result<int>.Failure("No artist nationalities were found in the database.");
				}

				return Result<int>.Success(count);
			}



			catch (Exception ex)
			{
				return Result<int>.Failure("An unknown error occured while FETCHING NATIONALITY COUNT from the database." + ex.Message);
			}
		}
		public async Task<Result<IEnumerable<Song>>> GetWantedSongsAsync()
		{
			try
			{
				await using var db = await _contextFactory.CreateDbContextAsync();
				var songs = await db.Songs
					.AsNoTracking()
					.Where(s => s.Wanted)
					.ToListAsync();

				if (!songs.Any())
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
				await using var db = await _contextFactory.CreateDbContextAsync();
				var genreCount = await db.Songs
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
				await using var db = await _contextFactory.CreateDbContextAsync();
				int year = DateTime.UtcNow.Year;

				// Two grouped queries instead of 24 round trips
				var songsPerMonth = await db.Songs
					.Where(s => s.SongCreatedAt.Year == year)
					.GroupBy(s => s.SongCreatedAt.Month)
					.Select(g => new { Month = g.Key, Count = g.Count() })
					.ToDictionaryAsync(x => x.Month, x => x.Count);

				var artistsPerMonth = await db.Artists
					.Where(a => a.ArtistCreatedAt.Year == year)
					.GroupBy(a => a.ArtistCreatedAt.Month)
					.Select(g => new { Month = g.Key, Count = g.Count() })
					.ToDictionaryAsync(x => x.Month, x => x.Count);

				var data = Enum.GetValues<Months>()
					.Select(month => new BarChartData
					{
						Month = month.ToString(),
						NewSongs = songsPerMonth.GetValueOrDefault((int)month),
						NewArtists = artistsPerMonth.GetValueOrDefault((int)month)
					})
					.ToList();

				return Result<List<BarChartData>>.Success(data);
			}

			catch(Exception ex)
			{
				return Result<List<BarChartData>>.Failure("Error loading monthly data: " + ex.Message);
			}

		}
	}
}
