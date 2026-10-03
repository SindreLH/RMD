using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RMD.Data.Context;
using RMD.Data.Models;
using RMD.Data.Models.Transfer;

namespace RMD.Business.Services
{
	public interface IDataTransferService
	{
		Task<ExportFile> ExportAsync();

		/// <summary>
		/// Appends the file's artists and songs to the database. With <paramref name="apply"/> false nothing
		/// is written and the summary says what would happen (preview).
		/// </summary>
		Task<ImportSummary> ImportAsync(ExportFile file, bool apply);

		/// <summary>Deletes all songs, artists and their links. Login data is never touched.</summary>
		Task<ClearSummary> ClearAsync();
	}

	public class DataTransferService : IDataTransferService
	{
		public const int MaxArtistNameLength = 200;
		public const int MaxGenreLength = 100;
		private const int MaxReportedProblems = 50;
		private static readonly Regex LengthFormat = new(@"^[0-5]?\d:[0-5]\d$");

		private readonly IDbContextFactory<RMDContext> _contextFactory;
		private readonly ILogger<DataTransferService> _logger;

		public DataTransferService(IDbContextFactory<RMDContext> contextFactory, ILogger<DataTransferService> logger)
		{
			_contextFactory = contextFactory;
			_logger = logger;
		}

		public async Task<ExportFile> ExportAsync()
		{
			await using var db = await _contextFactory.CreateDbContextAsync();

			var artists = await db.Artists.AsNoTracking()
				.OrderBy(a => a.ArtistId)
				.Select(a => new ExportArtist
				{
					Id = a.ArtistId,
					Name = a.Name,
					Nationality = a.Nationality,
					FacebookUrl = a.FacebookUrl,
					SoundcloudUrl = a.SoundcloudUrl,
					ProfilePicUrl = a.ProfilePicUrl,
					DiscogsUrl = a.DiscogsUrl,
					CreatedAt = a.ArtistCreatedAt,
					AliasGroup = a.AliasGroupId,
				})
				.ToListAsync();

			var songs = await db.Songs.AsNoTracking()
				.OrderBy(s => s.SongId)
				.Select(s => new ExportSong
				{
					Id = s.SongId,
					Title = s.Title,
					Length = s.Length,
					Genre = s.Genre,
					ExtendedMix = s.ExtendedMix,
					RadioMix = s.RadioMix,
					Played = s.Played,
					PlayedInEp = s.PlayedInEp,
					Stored = s.Stored,
					Wanted = s.Wanted,
					WantedSongUrl = s.WantedSongUrl,
					Favorite = s.Favorite,
					RemixArtist = s.RemixArtist,
					CreatedAt = s.SongCreatedAt,
					Artists = s.SongArtists
						.OrderBy(sa => sa.Role).ThenBy(sa => sa.ArtistId)
						.Select(sa => new ExportSongArtist { ArtistId = sa.ArtistId, Role = sa.Role })
						.ToList(),
				})
				.ToListAsync();

			return new ExportFile { ExportedAt = DateTime.UtcNow, Artists = artists, Songs = songs };
		}

		public async Task<ImportSummary> ImportAsync(ExportFile file, bool apply)
		{
			var summary = new ImportSummary();

			if (file.Format != ExportFile.FormatName || file.FormatVersion != ExportFile.CurrentVersion)
			{
				summary.Problems.Add($"Ukjent filformat ({file.Format} v{file.FormatVersion}). Forventet {ExportFile.FormatName} v{ExportFile.CurrentVersion}.");
				return summary;
			}

			await using var db = await _contextFactory.CreateDbContextAsync();

			// ---- Artists: reuse an existing artist with the same name, otherwise create one ----
			var existingByName = (await db.Artists.AsNoTracking().Select(a => new { a.ArtistId, a.Name }).ToListAsync())
				.GroupBy(a => NameKey(a.Name))
				.ToDictionary(g => g.Key, g => g.First().ArtistId);

			// file artist id -> existing database id, or a new (not yet saved) artist
			var artistRefs = new Dictionary<int, ArtistRef>();
			var newArtistsByName = new Dictionary<string, ArtistRef>();
			var nextTempId = -1;
			// file alias group -> the artists it maps to in this database
			var aliasGroups = new Dictionary<Guid, HashSet<ArtistRef>>();

			foreach (var fa in file.Artists)
			{
				var name = (fa.Name ?? "").Trim();
				if (name.Length == 0 || name.Length > MaxArtistNameLength)
				{
					Problem(summary, $"Artist #{fa.Id}: ugyldig navn.");
					continue;
				}

				if (artistRefs.ContainsKey(fa.Id))
				{
					Problem(summary, $"Artist #{fa.Id} finnes flere ganger i filen.");
					continue;
				}

				var key = NameKey(name);
				if (existingByName.TryGetValue(key, out var existingId))
				{
					artistRefs[fa.Id] = new ArtistRef(existingId, null);
					summary.ArtistsMatched++;
				}
				else if (newArtistsByName.TryGetValue(key, out var sameNameInFile))
				{
					artistRefs[fa.Id] = sameNameInFile;
				}
				else
				{
					var entity = new Artist
					{
						Name = name,
						Nationality = string.IsNullOrWhiteSpace(fa.Nationality) ? "Ukjent Opphav" : fa.Nationality.Trim(),
						FacebookUrl = SafeUrl.Normalize(fa.FacebookUrl),
						SoundcloudUrl = SafeUrl.Normalize(fa.SoundcloudUrl),
						ProfilePicUrl = SafeUrl.Normalize(fa.ProfilePicUrl),
						DiscogsUrl = SafeUrl.Normalize(fa.DiscogsUrl),
						ArtistCreatedAt = fa.CreatedAt ?? DateTime.UtcNow,
					};
					var reference = new ArtistRef(nextTempId--, entity);
					artistRefs[fa.Id] = reference;
					newArtistsByName[key] = reference;
					summary.ArtistsCreated++;
				}

				if (fa.AliasGroup is Guid fileGroup)
				{
					if (!aliasGroups.TryGetValue(fileGroup, out var members))
						aliasGroups[fileGroup] = members = new HashSet<ArtistRef>();
					members.Add(artistRefs[fa.Id]);
				}
			}

			// ---- Songs: same rules as SongService, duplicates (already in the database or in the file) are skipped ----
			var existingKeys = (await db.Songs.AsNoTracking()
					.Select(s => new
					{
						s.Title,
						s.ExtendedMix,
						s.RadioMix,
						Links = s.SongArtists.Select(sa => new { sa.ArtistId, sa.Role }).ToList()
					})
					.ToListAsync())
				.Select(s => SongKey(s.Title, s.ExtendedMix, s.RadioMix,
					s.Links.Where(l => l.Role == ArtistRole.Primary).Select(l => l.ArtistId),
					s.Links.Where(l => l.Role == ArtistRole.Remix).Select(l => l.ArtistId)))
				.ToHashSet();

			var newSongs = new List<Song>();

			foreach (var fs in file.Songs)
			{
				var label = $"Låt #{fs.Id} ({Shorten(fs.Title)})";
				var title = string.Join(' ', (fs.Title ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
				var genre = (fs.Genre ?? "").Trim();
				var length = (fs.Length ?? "").Trim();

				if (title.Length == 0 || title.Length > SongService.MaxTitleLength) { Problem(summary, $"{label}: ugyldig tittel."); continue; }
				if (genre.Length == 0 || genre.Length > MaxGenreLength) { Problem(summary, $"{label}: ugyldig sjanger."); continue; }
				if (!LengthFormat.IsMatch(length)) { Problem(summary, $"{label}: lengde må ha formatet MM:SS."); continue; }
				if (fs.PlayedInEp < 0) { Problem(summary, $"{label}: negativt episodenummer."); continue; }

				var links = (fs.Artists ?? new()).DistinctBy(a => (a.ArtistId, a.Role)).ToList();
				var missing = links.Where(l => !artistRefs.ContainsKey(l.ArtistId)).Select(l => l.ArtistId).ToList();
				if (missing.Count > 0) { Problem(summary, $"{label}: viser til artist som mangler i filen ({string.Join(", ", missing)})."); continue; }

				var primary = links.Where(l => l.Role == ArtistRole.Primary).Select(l => artistRefs[l.ArtistId]).Distinct().ToList();
				var remix = links.Where(l => l.Role == ArtistRole.Remix).Select(l => artistRefs[l.ArtistId]).Distinct().Except(primary).ToList();
				if (primary.Count == 0) { Problem(summary, $"{label}: mangler artist."); continue; }

				var key = SongKey(title, fs.ExtendedMix, fs.RadioMix, primary.Select(r => r.Id), remix.Select(r => r.Id));
				if (!existingKeys.Add(key))
				{
					summary.SongsSkippedDuplicate++;
					continue;
				}

				var song = new Song
				{
					Title = title,
					Length = length,
					Genre = genre,
					ExtendedMix = fs.ExtendedMix,
					RadioMix = fs.RadioMix,
					Played = fs.Played,
					PlayedInEp = fs.Played ? fs.PlayedInEp : null,
					Stored = fs.Stored,
					Wanted = fs.Wanted,
					WantedSongUrl = fs.Wanted ? SafeUrl.Normalize(fs.WantedSongUrl) : null,
					Favorite = fs.Favorite,
					RemixArtist = string.IsNullOrWhiteSpace(fs.RemixArtist) ? null : fs.RemixArtist.Trim(),
					SongCreatedAt = fs.CreatedAt ?? DateTime.UtcNow,
				};

				foreach (var r in primary) song.SongArtists.Add(r.Link(ArtistRole.Primary));
				foreach (var r in remix) song.SongArtists.Add(r.Link(ArtistRole.Remix));

				newSongs.Add(song);
			}

			summary.SongsCreated = newSongs.Count;

			if (!apply)
				return summary;

			// ---- Write everything in one transaction ----
			await using var transaction = await db.Database.BeginTransactionAsync();
			try
			{
				// Alias groups from the file get a fresh id here. Existing artists that already have
				// aliases keep them; a group needs at least two artists and holds at most 1 + MaxAliases.
				foreach (var members in aliasGroups.Values.Where(m => m.Count >= 2))
				{
					var existingIds = members.Where(m => m.NewArtist == null).Select(m => m.Id).ToList();
					var existingFree = await db.Artists
						.Where(a => existingIds.Contains(a.ArtistId) && a.AliasGroupId == null)
						.ToListAsync();

					var newMembers = members.Where(m => m.NewArtist != null).Select(m => m.NewArtist!).ToList();
					if (newMembers.Count + existingFree.Count < 2)
						continue;

					var group = Guid.NewGuid();
					foreach (var a in newMembers.Concat(existingFree).Take(ArtistService.MaxAliases + 1))
						a.AliasGroupId = group;
				}

				db.Artists.AddRange(newArtistsByName.Values.Select(r => r.NewArtist!));
				db.Songs.AddRange(newSongs);
				await db.SaveChangesAsync();
				await transaction.CommitAsync();
				summary.Applied = true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Import failed and was rolled back");
				await transaction.RollbackAsync();
				summary.Problems.Insert(0, "Importen feilet og ble rullet tilbake. Ingenting ble lagret.");
			}

			return summary;
		}

		public async Task<ClearSummary> ClearAsync()
		{
			await using var db = await _contextFactory.CreateDbContextAsync();
			await using var transaction = await db.Database.BeginTransactionAsync();

			var links = await db.SongArtists.ExecuteDeleteAsync();
			var songs = await db.Songs.ExecuteDeleteAsync();
			var artists = await db.Artists.ExecuteDeleteAsync();

			await transaction.CommitAsync();
			_logger.LogWarning("Database cleared: {Artists} artists, {Songs} songs, {Links} links", artists, songs, links);

			return new ClearSummary(artists, songs, links);
		}

		private static string NameKey(string name) => name.Trim().ToUpperInvariant();

		private static string SongKey(string title, bool extended, bool radio, IEnumerable<int> primary, IEnumerable<int> remix) =>
			string.Join('|',
				string.Join(' ', title.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant(),
				extended, radio,
				string.Join(',', primary.Distinct().OrderBy(i => i)),
				string.Join(',', remix.Distinct().OrderBy(i => i)));

		private static void Problem(ImportSummary summary, string message)
		{
			if (summary.Problems.Count < MaxReportedProblems)
				summary.Problems.Add(message);
			else if (summary.Problems.Count == MaxReportedProblems)
				summary.Problems.Add("... flere feil er utelatt.");
		}

		private static string Shorten(string? text) =>
			string.IsNullOrEmpty(text) ? "uten tittel" : text.Length > 30 ? text[..30] + "..." : text;

		/// <summary>An existing artist id, or a new artist with a temporary negative id used for duplicate checks.</summary>
		private sealed record ArtistRef(int Id, Artist? NewArtist)
		{
			public SongArtist Link(ArtistRole role) =>
				NewArtist != null
					? new SongArtist { Artist = NewArtist, Role = role }
					: new SongArtist { ArtistId = Id, Role = role };
		}
	}
}
