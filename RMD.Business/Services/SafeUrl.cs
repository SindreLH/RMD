namespace RMD.Business.Services
{
	/// <summary>
	/// User-entered links are rendered into href/src attributes, so only absolute http(s) urls are allowed.
	/// Anything else (e.g. "javascript:...") would run script in the logged-in session when clicked.
	/// </summary>
	public static class SafeUrl
	{
		/// <summary>
		/// For storing: trims, turns blank into null, adds https:// to scheme-less input like "www.discogs.com/x",
		/// and drops anything that is not an http(s) url.
		/// </summary>
		public static string? Normalize(string? url)
		{
			if (string.IsNullOrWhiteSpace(url))
				return null;

			var trimmed = url.Trim();

			if (!trimmed.Contains("://") && !trimmed.Contains(':'))
				trimmed = "https://" + trimmed;

			return ForHref(trimmed);
		}

		/// <summary>For rendering: the url if it is an absolute http(s) url, otherwise null.</summary>
		public static string? ForHref(string? url)
		{
			if (string.IsNullOrWhiteSpace(url))
				return null;

			var trimmed = url.Trim();

			return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
				&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
				? trimmed
				: null;
		}
	}
}
