using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMD.Business.Services;
using RMD.Data.Models.Transfer;
using RMD.GUI.Infrastructure;

namespace RMD.GUI.Controllers
{
	/// <summary>File downloads for the Settings page (plain links, so the browser handles the download).</summary>
	[ApiController]
	[Authorize]
	[Route("api/data")]
	public class DataController : ControllerBase
	{
		private readonly IDataTransferService _transfer;
		private readonly BackupStore _backups;

		public DataController(IDataTransferService transfer, BackupStore backups)
		{
			_transfer = transfer;
			_backups = backups;
		}

		/// <summary>All artists and songs as an RMD export file.</summary>
		[HttpGet("export")]
		public async Task<IActionResult> Export()
		{
			var file = await _transfer.ExportAsync();
			var bytes = JsonSerializer.SerializeToUtf8Bytes(file, ExportFile.JsonOptions);
			return File(bytes, "application/json", $"rmd-export-{DateTime.Now:yyyy-MM-dd}.json");
		}

		/// <summary>A backup saved before the database was cleared.</summary>
		[HttpGet("backups/{name}")]
		public IActionResult Backup(string name)
		{
			var stream = _backups.OpenRead(name);
			return stream == null ? NotFound() : File(stream, "application/json", name);
		}
	}
}
