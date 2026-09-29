using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RMD.Business.Services;
using RMD.Data.Models;
using RMD.GUI.Pages;
using System.Text;
using static RMD.GUI.Pages.ResetPassword;

namespace RMD.GUI.Controllers
{
	[Route("auth")]
	public class AuthController : Controller
	{
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IEmailService _emailService;

		public AuthController(
			SignInManager<ApplicationUser> signInManager,
			UserManager<ApplicationUser> userManager,
			IEmailService emailService)
		{
			_signInManager = signInManager;
			_userManager = userManager;
			_emailService = emailService;
		}

		[AllowAnonymous]
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromForm] LoginModel model, string? returnUrl = null)
		{
			var result = await _signInManager.PasswordSignInAsync(
				model.Email,
				model.Password,
				model.RememberMe, 
				lockoutOnFailure: false
			);

			if (!result.Succeeded)
				return Redirect("/?reason=invalid");

			if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
				return LocalRedirect(returnUrl);

			return Redirect("/dashboard");
		}

		[Authorize]
		[HttpPost("logout")]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return Redirect("/");
		}

		[AllowAnonymous]
		[HttpPost("ForgotPassword")]
		public async Task<IActionResult> SendPasswordResetLinkAsync([FromForm] ForgotPassword.ForgotPasswordModel model)
		{
			if (!ModelState.IsValid)
				return Redirect("/ForgotPassword?sent=true");

			

			var user = await _userManager.FindByEmailAsync(model.Email);
			if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
				return Redirect("/ForgotPassword?sent=true");

			var token = await _userManager.GeneratePasswordResetTokenAsync(user);

			//Encoding token, avoiding token errors. Used in var resetUrl
			var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

			var resetUrl =
				$"{Request.Scheme}://{Request.Host}/ResetPassword" +
				$"?token={encodedToken}" +
				$"&email={Uri.EscapeDataString(user.Email)}";

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
			);}

			catch (Exception ex)
			{
				Console.WriteLine("SMTP ERROR:");
				Console.WriteLine(ex.ToString());
			}

			return Redirect("/ForgotPassword?sent=true");
		}


		[HttpPost("ResetPassword")]
		public async Task<IActionResult> ResetPasswordAsync([FromForm] ResetPasswordModel model)
		{
			if (!ModelState.IsValid)
			{
				var modelErrors = ModelState.Values
					.SelectMany(v => v.Errors)
					.Select(e => e.ErrorMessage);

				var errorMessage = string.Join(" | ", modelErrors);

				return Redirect("/ResetPassword?error=" +
					Uri.EscapeDataString(errorMessage));
			}

			var user = await _userManager.FindByEmailAsync(model.Email);

			if (user == null)
			{
				return Redirect("/ResetPassword?reset=true");
			}
				
			var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));

			var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

			if (!result.Succeeded)
			{
				var errorMessage = string.Join(" | ", result.Errors.Select(e => e.Description));

				return Redirect(
					"/ResetPassword?token=" + Uri.EscapeDataString(model.Token) +
					"&email=" + Uri.EscapeDataString(model.Email) +
					"&error=" + Uri.EscapeDataString(errorMessage)
);
			}

			return Redirect("/?reset=success");
		}

	}
}
