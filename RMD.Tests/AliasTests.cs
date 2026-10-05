using System.Text.Json;
using RMD.Data.Models.Transfer;
using RMD.Tests.Infrastructure;

namespace RMD.Tests
{
	public class AliasTests
	{
		private static async Task<List<string>> AliasNames(TestDatabase db, int artistId) =>
			(await db.Artists().GetAliasesAsync(artistId)).Value.Select(a => a.Name).ToList();

		[Fact]
		public async Task Aliases_are_mutual()
		{
			using var db = new TestDatabase();
			var a = await db.AddArtistAsync("Brennan Heart");
			var b = await db.AddArtistAsync("B-Front");
			var c = await db.AddArtistAsync("I:Gor");

			var result = await db.Artists().SetAliasesAsync(a, [b, c]);

			Assert.True(result.IsSuccess, result.Error);
			Assert.Equal(["B-Front", "I:Gor"], await AliasNames(db, a));
			Assert.Equal(["Brennan Heart", "I:Gor"], await AliasNames(db, b));
		}

		[Fact]
		public async Task At_most_five_aliases_and_never_itself()
		{
			using var db = new TestDatabase();
			var main = await db.AddArtistAsync("Main");
			var others = new List<int>();
			for (var i = 0; i < 6; i++)
				others.Add(await db.AddArtistAsync($"Alias {i}"));

			Assert.False((await db.Artists().SetAliasesAsync(main, others)).IsSuccess);

			var five = await db.Artists().SetAliasesAsync(main, others.Take(5).Append(main));
			Assert.True(five.IsSuccess, five.Error);
			Assert.Equal(5, (await AliasNames(db, main)).Count);
		}

		[Fact]
		public async Task Removing_aliases_dissolves_the_group()
		{
			using var db = new TestDatabase();
			var a = await db.AddArtistAsync("A");
			var b = await db.AddArtistAsync("B");
			await db.Artists().SetAliasesAsync(a, [b]);

			await db.Artists().SetAliasesAsync(a, []);

			Assert.Empty(await AliasNames(db, a));
			Assert.Empty(await AliasNames(db, b));
			await using var ctx = db.CreateDbContext();
			Assert.All(ctx.Artists, x => Assert.Null(x.AliasGroupId));
		}

		[Fact]
		public async Task Picking_an_artist_from_another_group_moves_it_and_keeps_the_rest()
		{
			using var db = new TestDatabase();
			var a = await db.AddArtistAsync("A");
			var b = await db.AddArtistAsync("B");
			var c = await db.AddArtistAsync("C");
			var x = await db.AddArtistAsync("X");
			await db.Artists().SetAliasesAsync(a, [b, c]);

			await db.Artists().SetAliasesAsync(x, [c]);

			Assert.Equal(["C"], await AliasNames(db, x));
			Assert.Equal(["B"], await AliasNames(db, a));
		}

		[Fact]
		public async Task Deleting_an_artist_dissolves_a_group_left_with_one_member()
		{
			using var db = new TestDatabase();
			var a = await db.AddArtistAsync("A");
			var b = await db.AddArtistAsync("B");
			await db.Artists().SetAliasesAsync(a, [b]);

			await db.Artists().DeleteArtistByIdAsync(b);

			await using var ctx = db.CreateDbContext();
			Assert.Null(ctx.Artists.Single().AliasGroupId);
		}

		[Fact]
		public async Task Aliases_survive_export_and_import()
		{
			using var source = new TestDatabase();
			var a = await source.AddArtistAsync("A");
			var b = await source.AddArtistAsync("B");
			await source.AddArtistAsync("Solo");
			await source.Artists().SetAliasesAsync(a, [b]);

			var json = JsonSerializer.Serialize(await source.Transfer().ExportAsync(), ExportFile.JsonOptions);
			using var target = new TestDatabase();
			await target.Transfer().ImportAsync(JsonSerializer.Deserialize<ExportFile>(json, ExportFile.JsonOptions)!, apply: true);

			var importedA = (await target.Artists().GetArtistByNameAsync("A")).Value;
			Assert.Equal(["B"], await AliasNames(target, importedA.ArtistId));
			var solo = (await target.Artists().GetArtistByNameAsync("Solo")).Value;
			Assert.Null(solo.AliasGroupId);
		}
	}
}
