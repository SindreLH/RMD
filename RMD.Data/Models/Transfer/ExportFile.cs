using System.Text.Json;
using System.Text.Json.Serialization;

namespace RMD.Data.Models.Transfer
{
	/// <summary>
	/// The JSON file produced by Settings → Export and read by Import.
	/// Ids are only used to link songs to artists inside the file; import gives everything new ids.
	/// </summary>
	public sealed class ExportFile
	{
		public const string FormatName = "rmd-export";
		public const int CurrentVersion = 1;

		public string Format { get; set; } = FormatName;
		public int FormatVersion { get; set; } = CurrentVersion;
		public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
		public List<ExportArtist> Artists { get; set; } = new();
		public List<ExportSong> Songs { get; set; } = new();

		public static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true,
			DefaultIgnoreCondition = JsonIgnoreCondition.Never,
			Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
			// Emoji flags stay readable in the file
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		};
	}

	public sealed class ExportArtist
	{
		public int Id { get; set; }
		public string Name { get; set; } = "";
		public string Nationality { get; set; } = "";
		public string? FacebookUrl { get; set; }
		public string? SoundcloudUrl { get; set; }
		public string? ProfilePicUrl { get; set; }
		public string? DiscogsUrl { get; set; }
		public DateTime? CreatedAt { get; set; }
		/// <summary>Artists with the same value are aliases of each other. Optional (older files don't have it).</summary>
		public Guid? AliasGroup { get; set; }
	}

	public sealed class ExportSong
	{
		public int Id { get; set; }
		public string Title { get; set; } = "";
		public string Length { get; set; } = "";
		public string Genre { get; set; } = "";
		public bool ExtendedMix { get; set; }
		public bool RadioMix { get; set; }
		public bool Played { get; set; }
		public int? PlayedInEp { get; set; }
		public bool Stored { get; set; }
		public bool Wanted { get; set; }
		public string? WantedSongUrl { get; set; }
		public bool Favorite { get; set; }
		/// <summary>Legacy free-text remixer column, kept so nothing is lost in a round trip.</summary>
		public string? RemixArtist { get; set; }
		public DateTime? CreatedAt { get; set; }
		public List<ExportSongArtist> Artists { get; set; } = new();
	}

	public sealed class ExportSongArtist
	{
		/// <summary>Refers to <see cref="ExportArtist.Id"/> in the same file.</summary>
		public int ArtistId { get; set; }
		public ArtistRole Role { get; set; }
	}

	/// <summary>What an import did (or would do, for a preview).</summary>
	public sealed class ImportSummary
	{
		public bool Applied { get; set; }
		public int ArtistsCreated { get; set; }
		public int ArtistsMatched { get; set; }
		public int SongsCreated { get; set; }
		public int SongsSkippedDuplicate { get; set; }
		public List<string> Problems { get; } = new();
	}

	/// <summary>Rows removed by Clear database.</summary>
	public sealed record ClearSummary(int Artists, int Songs, int Links);
}
