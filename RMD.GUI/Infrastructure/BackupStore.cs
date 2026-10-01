using System.Text.Json;
using System.Text.RegularExpressions;
using RMD.Data.Models.Transfer;

namespace RMD.GUI.Infrastructure
{
	/// <summary>
	/// JSON backups written before "Tøm database". Folder: Backup:Directory, default App_Data/backups.
	/// On Azure App Service use a folder under /home (it survives restarts and deployments).
	/// </summary>
	public sealed class BackupStore
	{
		private static readonly Regex FileNamePattern = new(@"^rmd-backup-\d{8}-\d{6}\.json$");
		private readonly string _directory;

		public BackupStore(IConfiguration config, IWebHostEnvironment env)
		{
			var configured = config["Backup:Directory"];
			_directory = string.IsNullOrWhiteSpace(configured)
				? Path.Combine(env.ContentRootPath, "App_Data", "backups")
				: configured;
		}

		public async Task<string> SaveAsync(ExportFile file)
		{
			Directory.CreateDirectory(_directory);
			var name = $"rmd-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
			await using var stream = File.Create(Path.Combine(_directory, name));
			await JsonSerializer.SerializeAsync(stream, file, ExportFile.JsonOptions);
			return name;
		}

		public IReadOnlyList<BackupInfo> List()
		{
			if (!Directory.Exists(_directory))
				return Array.Empty<BackupInfo>();

			return new DirectoryInfo(_directory)
				.GetFiles("rmd-backup-*.json")
				.Where(f => FileNamePattern.IsMatch(f.Name))
				.OrderByDescending(f => f.Name)
				.Select(f => new BackupInfo(f.Name, f.Length, f.LastWriteTimeUtc))
				.ToList();
		}

		/// <summary>Opens a backup by name; null for unknown or malformed names (no path traversal).</summary>
		public Stream? OpenRead(string name)
		{
			if (!FileNamePattern.IsMatch(name))
				return null;

			var path = Path.Combine(_directory, name);
			return File.Exists(path) ? File.OpenRead(path) : null;
		}
	}

	public sealed record BackupInfo(string Name, long Size, DateTime CreatedUtc);
}
