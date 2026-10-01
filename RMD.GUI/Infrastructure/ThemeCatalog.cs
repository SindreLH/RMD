namespace RMD.GUI.Infrastructure
{
	/// <summary>
	/// The available colour themes. Each id matches a [data-theme] block in wwwroot/css/themes.css.
	/// The chosen id is stored in the "rmd-theme" cookie and rendered on &lt;html&gt; by _Layout.cshtml.
	/// </summary>
	public static class ThemeCatalog
	{
		public const string CookieName = "rmd-theme";
		public const string DefaultId = "standard";

		public static readonly IReadOnlyList<ThemeInfo> All = new[]
		{
			new ThemeInfo("standard", "Standard", "Blått og grønt, det opprinnelige RMD-utseendet", false,
				new[] { "#1f78d7", "#25369b", "#43c000", "#212529" },
				new ChartColors("#ffffff", null, null)),
			new ThemeInfo("midnight", "Midnight", "Nesten svart med grønn aksent", true,
				new[] { "#121212", "#282828", "#1db954", "#b3b3b3" },
				new ChartColors("#b3b3b3", "#2a2a2a", new[] { "#1db954", "#509bf5", "#f59b23", "#e8115b", "#af2896", "#4ac8e8", "#ffc864", "#b49bc8" })),
			new ThemeInfo("daylight", "Daylight", "Lyst tema", false,
				new[] { "#f4f6fb", "#ffffff", "#2563eb", "#15803d" },
				new ChartColors("#475569", "#e2e8f0", new[] { "#2563eb", "#16a34a", "#d97706", "#dc2626", "#7c3aed", "#0891b2", "#db2777", "#65a30d" })),
			new ThemeInfo("nord", "Nord", "Kjølig skifergrå", true,
				new[] { "#2e3440", "#3b4252", "#88c0d0", "#a3be8c" },
				new ChartColors("#d8dee9", "#4c566a", new[] { "#88c0d0", "#a3be8c", "#ebcb8b", "#bf616a", "#b48ead", "#5e81ac", "#d08770", "#8fbcbb" })),
			new ThemeInfo("synthwave", "Synthwave", "Dyp lilla med neonrosa og cyan", true,
				new[] { "#1a1033", "#2a1b4d", "#ff4fd8", "#36f9f6" },
				new ChartColors("#c4b5e8", "#3d2a6e", new[] { "#ff4fd8", "#36f9f6", "#ffd166", "#ff5470", "#9d7bff", "#7cff6b", "#ff9e40", "#4d9dff" })),
		};

		public static ThemeInfo Get(string? id) => All.First(t => t.Id == Normalize(id));

		/// <summary>Returns a known theme id, falling back to the default for missing or unknown values.</summary>
		public static string Normalize(string? id) =>
			All.Any(t => t.Id == id) ? id! : DefaultId;
	}

	public sealed record ThemeInfo(string Id, string Name, string Description, bool IsDark, string[] Swatches, ChartColors Chart);

	/// <summary>ApexCharts colours for a theme. Null keeps ApexCharts' defaults (used by Standard).</summary>
	public sealed record ChartColors(string Text, string? Grid, string[]? Palette);
}
