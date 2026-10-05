using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RMD.Business.Services;
using RMD.Data.Context;
using RMD.Data.Models.DTO;

namespace RMD.Tests.Infrastructure
{
	/// <summary>
	/// A fresh in-memory SQLite database per test, exposed through the same IDbContextFactory the
	/// services use in production. Note: SQLite compares strings case-sensitively (SQL Server doesn't).
	/// </summary>
	public sealed class TestDatabase : IDbContextFactory<RMDContext>, IDisposable
	{
		private readonly SqliteConnection _connection;
		private readonly DbContextOptions<RMDContext> _options;

		public TestDatabase()
		{
			_connection = new SqliteConnection("DataSource=:memory:");
			_connection.Open();
			_options = new DbContextOptionsBuilder<RMDContext>().UseSqlite(_connection).Options;

			using var db = CreateDbContext();
			db.Database.EnsureCreated();
		}

		public RMDContext CreateDbContext() => new(_options);

		public ArtistService Artists() => new(this, NullLogger<ArtistService>.Instance);
		public SongService Songs() => new(this, NullLogger<SongService>.Instance);
		public DataTransferService Transfer() => new(this, NullLogger<DataTransferService>.Instance);

		public async Task<int> AddArtistAsync(string name, string nationality = "🇳🇴 Norge")
		{
			var result = await Artists().CreateNewArtistAsync(new ArtistDto { Name = name, Nationality = nationality });
			Assert.True(result.IsSuccess, result.Error);
			return result.Value.ArtistId;
		}

		public static SongDto Song(string title, int[] artists, int[]? remixers = null, bool extended = false) => new()
		{
			Title = title,
			Length = "3:45",
			Genre = "Hands Up",
			ExtendedMix = extended,
			ArtistIds = artists.ToList(),
			RemixArtistIds = (remixers ?? Array.Empty<int>()).ToList(),
		};

		public void Dispose() => _connection.Dispose();
	}
}
