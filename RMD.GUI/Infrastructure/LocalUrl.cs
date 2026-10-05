namespace RMD.GUI.Infrastructure
{
	public static class LocalUrl
	{
		/// <summary>
		/// Returns the url if it is a safe app-relative path (e.g. "/Artists?x=1"), otherwise the fallback.
		/// Rejects absolute urls, protocol-relative "//host" and "/\host" to prevent open redirects.
		/// </summary>
		public static string OrDefault(string? url, string fallback = "/dashboard")
		{
			if (string.IsNullOrWhiteSpace(url))
				return fallback;

			var isLocal = url.StartsWith('/')
				&& !url.StartsWith("//")
				&& !url.StartsWith("/\\");

			if (!isLocal || url.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
				return fallback;

			return url;
		}
	}
}
