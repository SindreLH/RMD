using Microsoft.EntityFrameworkCore;
using RMD.Data.Models;
using RMD.Tests.Infrastructure;

namespace RMD.Tests
{
	public class SongServiceTests
	{
		[Fact]
		public async Task Exact_duplicate_is_rejected()
		{
			using var db = new TestDatabase();
			var artist = await db.AddArtistAsync("Cascada");
			var songs = db.Songs();

			Assert.True((await songs.CreateNewSongAsync(TestDatabase.Song("Heaven", [artist]))).IsSuccess);
			var duplicate = await songs.CreateNewSongAsync(TestDatabase.Song("  Heaven ", [artist]));

			Assert.False(duplicate.IsSuccess);
			Assert.Contains("already exists", duplicate.Error);
		}

		[Fact]
		public async Task Same_title_is_allowed_for_remix_other_artist_and_extended_version()
		{
			using var db = new TestDatabase();
			var a = await db.AddArtistAsync("Artist A");
			var b = await db.AddArtistAsync("Artist B");
			var remixer = await db.AddArtistAsync("Remixer");
			var songs = db.Songs();

			Assert.True((await songs.CreateNewSongAsync(TestDatabase.Song("Heaven", [a]))).IsSuccess);
			Assert.True((await songs.CreateNewSongAsync(TestDatabase.Song("Heaven", [a], [remixer]))).IsSuccess);
			Assert.True((await songs.CreateNewSongAsync(TestDatabase.Song("Heaven", [b]))).IsSuccess);
			Assert.True((await songs.CreateNewSongAsync(TestDatabase.Song("Heaven", [a], extended: true))).IsSuccess);
		}

		[Fact]
		public async Task Song_needs_an_existing_artist()
		{
			using var db = new TestDatabase();
			var artist = await db.AddArtistAsync("Artist");
			var songs = db.Songs();

			Assert.False((await songs.CreateNewSongAsync(TestDatabase.Song("No artist", []))).IsSuccess);
			Assert.False((await songs.CreateNewSongAsync(TestDatabase.Song("Ghost", [artist, 999]))).IsSuccess);
		}

		[Fact]
		public async Task Invalid_length_is_rejected()
		{
			using var db = new TestDatabase();
			var artist = await db.AddArtistAsync("Artist");
			var dto = TestDatabase.Song("Track", [artist]);
			dto.Length = "abc";

			Assert.False((await db.Songs().CreateNewSongAsync(dto)).IsSuccess);
		}

		[Fact]
		public async Task Episode_and_source_url_are_only_kept_when_the_flags_are_set()
		{
			using var db = new TestDatabase();
			var artist = await db.AddArtistAsync("Artist");
			var dto = TestDatabase.Song("Track", [artist]);
			dto.Played = false;
			dto.PlayedInEp = 7;
			dto.Wanted = false;
			dto.WantedSongUrl = "https://example.com";

			var result = await db.Songs().CreateNewSongAsync(dto);

			Assert.True(result.IsSuccess, result.Error);
			Assert.Null(result.Value.PlayedInEp);
			Assert.Null(result.Value.WantedSongUrl);
		}

		[Fact]
		public async Task Update_replaces_artist_links_and_rejects_a_duplicate_rename()
		{
			using var db = new TestDatabase();
			var a = await db.AddArtistAsync("Artist A");
			var remixer = await db.AddArtistAsync("Remixer");
			var songs = db.Songs();

			var first = (await songs.CreateNewSongAsync(TestDatabase.Song("First", [a]))).Value;
			var second = (await songs.CreateNewSongAsync(TestDatabase.Song("Second", [a], [remixer]))).Value;

			// Removing the remixer
			var updated = await songs.UpdateSongByIdAsync(second.SongId, TestDatabase.Song("Second", [a]));
			Assert.True(updated.IsSuccess, updated.Error);
			await using (var ctx = db.CreateDbContext())
			{
				var links = await ctx.SongArtists.Where(sa => sa.SongId == second.SongId).ToListAsync();
				Assert.Single(links);
				Assert.Equal(ArtistRole.Primary, links[0].Role);
			}

			// Renaming the second song into a copy of the first
			var clash = await songs.UpdateSongByIdAsync(second.SongId, TestDatabase.Song("First", [a]));
			Assert.False(clash.IsSuccess);
		}
	}
}
