using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RMD.GUI.Infrastructure;

namespace RMD.GUI.Controllers
{
	[Authorize]
	[Route("settings")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class SettingsController : Controller
	{
		/// <summary>
		/// Stores the chosen theme in a cookie and reloads Settings. A full page load means _Layout
		/// renders the new data-theme and the charts pick up the new colours too.
		/// </summary>
		[HttpPost("theme")]
		[ValidateAntiForgeryToken]
		public IActionResult SetTheme([FromForm] string theme)
		{
			Response.Cookies.Append(ThemeCatalog.CookieName, ThemeCatalog.Normalize(theme), new CookieOptions
			{
				Expires = DateTimeOffset.UtcNow.AddYears(1),
				HttpOnly = true,
				Secure = true,
				SameSite = SameSiteMode.Lax,
				IsEssential = true,
			});

			return LocalRedirect("/Settings?saved=theme");
		}
	}
}
