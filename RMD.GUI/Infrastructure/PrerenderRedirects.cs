using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RMD.GUI.Infrastructure
{
	/// <summary>
	/// Does the common page redirects as plain HTTP redirects before Blazor renders anything.
	/// A NavigateTo() during Blazor Server prerendering works, but only by throwing a NavigationException
	/// that the framework catches; the debugger stops on it every time. The components keep their own
	/// NavigateTo() for in-app navigation, where no exception is involved.
	///   /                              -> /dashboard
	///   protected page, not logged in  -> /login?returnUrl=...
	///   /login, already logged in      -> returnUrl or /dashboard
	/// </summary>
	public static class PrerenderRedirects
	{
		// Routes of components marked [AllowAnonymous] (login, forgot/reset password, 404, the "/" forwarder)
		private static readonly HashSet<string> AnonymousRoutes = typeof(PrerenderRedirects).Assembly
			.GetTypes()
			.Where(t => typeof(IComponent).IsAssignableFrom(t) && t.GetCustomAttribute<AllowAnonymousAttribute>() != null)
			.SelectMany(t => t.GetCustomAttributes<RouteAttribute>())
			.Select(r => "/" + r.Template.Trim('/'))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		public static IApplicationBuilder UsePrerenderRedirects(this IApplicationBuilder app)
		{
			return app.Use(async (context, next) =>
			{
				// Only requests that end up in the Blazor host page (_Host.cshtml)
				var isBlazorPage = context.GetEndpoint()?.Metadata.GetMetadata<PageActionDescriptor>()?.ViewEnginePath == "/_Host";
				if (!isBlazorPage || !HttpMethods.IsGet(context.Request.Method))
				{
					await next();
					return;
				}

				var path = context.Request.Path.Value ?? "/";
				var signedIn = context.User.Identity?.IsAuthenticated == true;

				if (path == "/")
				{
					context.Response.Redirect("/dashboard");
					return;
				}

				if (signedIn && path.Equals("/login", StringComparison.OrdinalIgnoreCase))
				{
					context.Response.Redirect(LocalUrl.OrDefault(context.Request.Query["returnUrl"]));
					return;
				}

				if (!signedIn && !AnonymousRoutes.Contains(path.TrimEnd('/')))
				{
					var returnUrl = path + context.Request.QueryString;
					context.Response.Redirect("/login?returnUrl=" + Uri.EscapeDataString(returnUrl));
					return;
				}

				await next();
			});
		}
	}
}
