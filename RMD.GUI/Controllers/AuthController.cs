using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using RMD.Business.Services;
using RMD.Data.Models;
using RMD.GUI.Infrastructure;
using RMD.GUI.Pages;
using System.Text;
using static RMD.GUI.Pages.ResetPassword;

namespace RMD.GUI.Controllers
{
	[Route("auth")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class AuthController : Controller
	{
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IEmailService _emailService;
		private readonly IConfiguration _config;
		private readonly IWebHostEnvironment _env;
		private readonly ILogger<AuthController> _logger;

		public AuthController(
			SignInManager<ApplicationUser> signInManager,
			UserManager<ApplicationUser> userManager,
			IEmailService emailService,
			IConfiguration config,
			IWebHostEnvironment env,
			ILogger<AuthController> logger)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_emailService = emailService;
			_config = config;
			_env = env;
			_logger = logger;
		}

		[AllowAnonymous]
		[HttpPost("login")]
		[ValidateAntiForgeryToken]
		[EnableRateLimiting(RateLimitPolicies.Login)]
		public async Task<IActionResult> Login([FromForm] LoginModel model, [FromForm] string? returnUrl = null)
		{
			var target = LocalUrl.OrDefault(returnUrl);

			if (!ModelState.IsValid)
				return Redirect(LoginUrl("invalid", target));

			var result = await _signInManager.PasswordSignInAsync(
				model.Email,
				model.Password,
				model.RememberMe,
				lockoutOnFailure: true
			);

			if (result.IsLockedOut)
			{
				_logger.LogWarning("Login locked out for {Email}", model.Email);
				return Redirect(LoginUrl("locked", target));
			}

			if (!result.Succeeded)
			{
				_logger.LogWarning("Failed login for {Email}", model.Email);
				return Redirect(LoginUrl("invalid", target));
			}

			return LocalRedirect(target);
		}

		[Authorize]
		[HttpPost("logout")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Logout()
		{
			// Rotating the security stamp signs out every other open tab and device as well
			var user = await _userManager.GetUserAsync(User);
			if (user != null)
				await _userManager.UpdateSecurityStampAsync(user);

			await _signInManager.SignOutAsync();
			return Redirect("/login");
		}

		[AllowAnonymous]
		[HttpPost("ForgotPassword")]
		[ValidateAntiForgeryToken]
		[EnableRateLimiting(RateLimitPolicies.PasswordReset)]
		public async Task<IActionResult> SendPasswordResetLinkAsync([FromForm] ForgotPassword.ForgotPasswordModel model)
		{
			// Always answer the same way so the form does not reveal which e-mails exist
			const string done = "/ForgotPassword?sent=true";

			if (!ModelState.IsValid)
				return Redirect(done);

			var user = await _userManager.FindByEmailAsync(model.Email);
			if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
				return Redirect(done);

			var baseUrl = PublicBaseUrl();
			if (baseUrl == null)
			{
				_logger.LogError("App:PublicBaseUrl is not configured; password reset e-mail not sent");
				return Redirect(done);
			}

			var token = await _userManager.GeneratePasswordResetTokenAsync(user);

			//Encoding token, avoiding token errors. Used in var resetUrl
			var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

			var resetUrl =
				$"{baseUrl}/ResetPassword" +
				$"?token={encodedToken}" +
				$"&email={Uri.EscapeDataString(user.Email!)}";

			var htmlMessage =
				$@"
				<h2>Tilbakestill passord</h2>
				<p>Klikk på lenken for tilbakestilling av ditt passord.</p>
				<p>
					<a href=""{resetUrl}"">Tilbakestill passord</a>
				</p>";

			try
			{
				await _emailService.SendEmailAsync(
					user.Email!,
					"RMD Passordendring",
					htmlMessage
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send password reset e-mail");
			}

			return Redirect(done);
		}

		[AllowAnonymous]
		[HttpPost("ResetPassword")]
		[ValidateAntiForgeryToken]
		[EnableRateLimiting(RateLimitPolicies.PasswordReset)]
		public async Task<IActionResult> ResetPasswordAsync([FromForm] ResetPasswordModel model)
		{
			if (!ModelState.IsValid)
			{
				var modelErrors = ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage);

				return Redirect(ResetUrl(model, string.Join("|", modelErrors)));
			}

			var user = await _userManager.FindByEmailAsync(model.Email);
			if (user == null)
				return Redirect(ResetUrl(model, "Ugyldig eller utløpt reset-lenke."));

			string decodedToken;
			try
			{
				decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
			}
			catch (FormatException)
			{
				return Redirect(ResetUrl(model, "Ugyldig eller utløpt reset-lenke."));
			}

			var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

			if (!result.Succeeded)
				return Redirect(ResetUrl(model, string.Join("|", result.Errors.Select(e => e.Description))));

			return Redirect("/login?reset=success");
		}

		private static string LoginUrl(string reason, string returnUrl) =>
			$"/login?reason={reason}&returnUrl={Uri.EscapeDataString(returnUrl)}";

		private static string ResetUrl(ResetPasswordModel model, string error) =>
			"/ResetPassword?token=" + Uri.EscapeDataString(model.Token ?? "") +
			"&email=" + Uri.EscapeDataString(model.Email ?? "") +
			"&error=" + Uri.EscapeDataString(error);

		/// <summary>
		/// Reset links must never be built from the request Host header (it can be forged).
		/// Production uses App:PublicBaseUrl; Development falls back to the current host.
		/// </summary>
		private string? PublicBaseUrl()
		{
			var configured = _config["App:PublicBaseUrl"];
			if (!string.IsNullOrWhiteSpace(configured))
				return configured.TrimEnd('/');

			return _env.IsDevelopment() ? $"{Request.Scheme}://{Request.Host}" : null;
		}
	}
}
