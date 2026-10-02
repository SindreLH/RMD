using RMD.Business.Services;
using RMD.GUI.Infrastructure;

namespace RMD.Tests
{
	public class HelperTests
	{
		[Theory]
		[InlineData("https://www.discogs.com/x", "https://www.discogs.com/x")]
		[InlineData("http://example.com", "http://example.com")]
		[InlineData("javascript:alert(1)", null)]
		[InlineData("data:text/html,hi", null)]
		[InlineData("", null)]
		[InlineData(null, null)]
		public void SafeUrl_ForHref_allows_only_http_and_https(string? input, string? expected)
		{
			Assert.Equal(expected, SafeUrl.ForHref(input));
		}

		[Theory]
		[InlineData("/Artists?x=1", "/Artists?x=1")]
		[InlineData("//evil.com", "/dashboard")]
		[InlineData("/\\evil.com", "/dashboard")]
		[InlineData("https://evil.com", "/dashboard")]
		[InlineData("/login?returnUrl=/x", "/dashboard")]
		[InlineData(null, "/dashboard")]
		public void LocalUrl_only_allows_app_relative_paths(string? input, string expected)
		{
			Assert.Equal(expected, LocalUrl.OrDefault(input));
		}

		[Theory]
		[InlineData("midnight", "midnight")]
		[InlineData("not-a-theme", "standard")]
		[InlineData(null, "standard")]
		public void ThemeCatalog_falls_back_to_standard(string? input, string expected)
		{
			Assert.Equal(expected, ThemeCatalog.Normalize(input));
		}
	}
}
