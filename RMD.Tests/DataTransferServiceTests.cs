using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RMD.Data.Models;
using RMD.Data.Models.Transfer;
using RMD.Tests.Infrastructure;

namespace RMD.Tests
{
	public class DataTransferServiceTests
	{
		[Fact]
		public async Task Export_then_import_into_an_empty_database_restores_everything()
		{
			using var source = new TestDatabase();
			var a = await source.AddArtistAsync("Brennan Heart", "🇳🇱 Nederland");
			var r = await source.AddArtistAsync("Remixer");
			await source.Songs().CreateNewSongAsync(TestDatabase.Song("Imaginary", [a], [r]));
			await source.Songs().CreateNewSongAsync(TestDatabase.Song("Lose My Mind", [a]));

			// Through JSON, like the real download/upload
			var json = JsonSerializer.Serialize(await source.Transfer().ExportAsync(), ExportFile.JsonOptions);
			var file = JsonSerializer.Deserialize<ExportFile>(json, ExportFile.JsonOptions)!;

			using var target = new TestDatabase();
			var summary = await target.Transfer().ImportAsync(file, apply: true);

			Assert.True(summary.Applied);
			Assert.Equal(2, summary.ArtistsCreated);
			Assert.Equal(2, summary.SongsCreated);
			Assert.Empty(summary.Problems);

			await using var ctx = target.CreateDbContext();
			var imaginary = await ctx.Songs.Include(s => s.SongArtists).ThenInclude(sa => sa.Artist)
				.SingleAsync(s => s.Title == "Imaginary");
			Assert.Contains(imaginary.SongArtists, sa => sa.Role == ArtistRole.Primary && sa.Artist.Name == "Brennan Heart");
			Assert.Contains(imaginary.SongArtists, sa => sa.Role == ArtistRole.Remix && sa.Artist.Name == "Remixer");
			Assert.Equal("🇳🇱 Nederland", (await ctx.Artists.SingleAsync(x => x.Name == "Brennan Heart")).Nationality);
		}

		[Fact]
		public async Task Import_appends_reuses_artists_skips_duplicates_and_reports_bad_rows()
		{
			using var db = new TestDatabase();
			var existing = await db.AddArtistAsync("DJ Test");
			await db.Songs().CreateNewSongAsync(TestDatabase.Song("Heaven", [existing]));

			var file = new ExportFile
			{
				Artists =
				{
					new ExportArtist { Id = 1, Name = "  dj TEST " },                                     // same artist, other casing
					new ExportArtist { Id = 2, Name = "New Artist", SoundcloudUrl = "javascript:alert(1)" },
				},
				Songs =
				{
					Song(10, "HEAVEN", (1, ArtistRole.Primary)),                         // duplicate of the existing song
					Song(11, "Fresh  Track", (2, ArtistRole.Primary), (1, ArtistRole.Remix)),
					Song(12, "Bad length", (2, ArtistRole.Primary), length: "abc"),
					Song(13, "Ghost", (99, ArtistRole.Primary)),
				},
			};

			var preview = await db.Transfer().ImportAsync(file, apply: false);
			Assert.False(preview.Applied);
			Assert.Equal(1, preview.ArtistsCreated);
			Assert.Equal(1, preview.ArtistsMatched);
			Assert.Equal(1, preview.SongsCreated);
			Assert.Equal(1, preview.SongsSkippedDuplicate);
			Assert.Equal(2, preview.Problems.Count);

			await using (var ctx = db.CreateDbContext())
				Assert.Equal(1, await ctx.Songs.CountAsync()); // preview wrote nothing

			var applied = await db.Transfer().ImportAsync(file, apply: true);
			Assert.True(applied.Applied);

			await using (var ctx = db.CreateDbContext())
			{
				Assert.Equal(2, await ctx.Artists.CountAsync());
				Assert.Null((await ctx.Artists.SingleAsync(a => a.Name == "New Artist")).SoundcloudUrl);

				var fresh = await ctx.Songs.Include(s => s.SongArtists).SingleAsync(s => s.Title == "Fresh Track");
				Assert.Contains(fresh.SongArtists, sa => sa.Role == ArtistRole.Remix && sa.ArtistId == existing);
			}
		}

		[Fact]
		public async Task Unknown_format_is_refused()
		{
			using var db = new TestDatabase();
			var summary = await db.Transfer().ImportAsync(new ExportFile { Format = "something-else" }, apply: true);

			Assert.False(summary.Applied);
			Assert.Single(summary.Problems);
		}

		[Fact]
		public async Task Clear_removes_songs_artists_and_links()
		{
			using var db = new TestDatabase();
			var a = await db.AddArtistAsync("Artist");
			await db.Songs().CreateNewSongAsync(TestDatabase.Song("Track", [a]));

			var result = await db.Transfer().ClearAsync();

			Assert.Equal(new ClearSummary(1, 1, 1), result);
			await using var ctx = db.CreateDbContext();
			Assert.False(await ctx.Artists.AnyAsync());
			Assert.False(await ctx.Songs.AnyAsync());
		}

		private static ExportSong Song(int id, string title, (int ArtistId, ArtistRole Role) first, (int ArtistId, ArtistRole Role)? second = null, string length = "3:30")
		{
			var song = new ExportSong { Id = id, Title = title, Length = length, Genre = "Hands Up" };
			song.Artists.Add(new ExportSongArtist { ArtistId = first.ArtistId, Role = first.Role });
			if (second is { } s)
				song.Artists.Add(new ExportSongArtist { ArtistId = s.ArtistId, Role = s.Role });
			return song;
		}
	}
}
