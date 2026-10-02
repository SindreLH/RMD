using RMD.Data.Models.DTO;
using RMD.Tests.Infrastructure;

namespace RMD.Tests
{
	public class ArtistServiceTests
	{
		[Fact]
		public async Task Names_are_trimmed_and_kept_unique()
		{
			using var db = new TestDatabase();
			var artists = db.Artists();

			var created = await artists.CreateNewArtistAsync(new ArtistDto { Name = "  Cascada ", Nationality = "🇩🇪 Tyskland" });
			Assert.Equal("Cascada", created.Value.Name);

			var duplicate = await artists.CreateNewArtistAsync(new ArtistDto { Name = "Cascada", Nationality = "🇩🇪 Tyskland" });
			Assert.False(duplicate.IsSuccess);
		}

		[Fact]
		public async Task Rename_to_an_existing_name_is_rejected()
		{
			using var db = new TestDatabase();
			await db.AddArtistAsync("Artist A");
			var b = await db.AddArtistAsync("Artist B");

			var result = await db.Artists().UpdateArtistByIdAsync(b, new ArtistDto { Name = "Artist A", Nationality = "🇳🇴 Norge" });

			Assert.False(result.IsSuccess);
		}

		[Fact]
		public async Task Unsafe_links_are_dropped_and_scheme_less_links_get_https()
		{
			using var db = new TestDatabase();

			var result = await db.Artists().CreateNewArtistAsync(new ArtistDto
			{
				Name = "Linked",
				Nationality = "🇳🇴 Norge",
				FacebookUrl = "javascript:alert(1)",
				DiscogsUrl = "www.discogs.com/artist/1",
				SoundcloudUrl = "",
			});

			Assert.Null(result.Value.FacebookUrl);
			Assert.Equal("https://www.discogs.com/artist/1", result.Value.DiscogsUrl);
			Assert.Null(result.Value.SoundcloudUrl);
		}

		[Fact]
		public async Task Counts_songs_that_would_be_left_without_an_artist()
		{
			using var db = new TestDatabase();
			var a = await db.AddArtistAsync("Artist A");
			var b = await db.AddArtistAsync("Artist B");
			var songs = db.Songs();
			await songs.CreateNewSongAsync(TestDatabase.Song("Only A", [a]));
			await songs.CreateNewSongAsync(TestDatabase.Song("A and B", [a, b]));
			await songs.CreateNewSongAsync(TestDatabase.Song("B with A remix", [b], [a]));

			var count = await db.Artists().CountSongsLeftWithoutArtistAsync(a);

			Assert.Equal(1, count.Value);
		}
	}
}
