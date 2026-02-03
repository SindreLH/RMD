using Microsoft.AspNetCore.Identity;
using RMD.Data.Models;

namespace RMD.Business.Services
{
	public class AuthService
	{
		private readonly SignInManager<ApplicationUser> _signInManager;

		public AuthService(SignInManager<ApplicationUser> signInManager)
		{
			_signInManager = signInManager;
		}

		public async Task<Result<bool>> Login(string email, string password)
		{
			var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: true);

			if (result.IsLockedOut)
			{
				return Result<bool>.Failure("Wrong credentials entered too many times. User has been locked out.");
			}


			if (!result.Succeeded)
			{
				return Result<bool>.Failure("Wrong credentials entered. Check e-mail and/or password.");
			}

			return Result<bool>.Success(true);
		}

		public async Task Logout()
		{
			await _signInManager.SignOutAsync();
		}
	}
}
