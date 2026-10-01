using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace RMD.GUI.Infrastructure
{
	/// <summary>
	/// A Blazor circuit keeps the user it started with for its whole lifetime.
	/// This provider re-checks the Identity security stamp every minute, so logging out
	/// (which rotates the stamp) or resetting the password also ends sessions in other open tabs.
	/// </summary>
	public sealed class IdentityRevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
	{
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly IdentityOptions _options;

		public IdentityRevalidatingAuthenticationStateProvider(
			ILoggerFactory loggerFactory,
			IServiceScopeFactory scopeFactory,
			IOptions<IdentityOptions> optionsAccessor)
			: base(loggerFactory)
		{
			_scopeFactory = scopeFactory;
			_options = optionsAccessor.Value;
		}

		protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(1);

		protected override async Task<bool> ValidateAuthenticationStateAsync(
			AuthenticationState authenticationState, CancellationToken cancellationToken)
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
			return await ValidateSecurityStampAsync(userManager, authenticationState.User);
		}

		private async Task<bool> ValidateSecurityStampAsync(UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
		{
			var user = await userManager.GetUserAsync(principal);
			if (user is null)
				return false;

			if (!userManager.SupportsUserSecurityStamp)
				return true;

			var principalStamp = principal.FindFirstValue(_options.ClaimsIdentity.SecurityStampClaimType);
			var userStamp = await userManager.GetSecurityStampAsync(user);
			return principalStamp == userStamp;
		}
	}
}
