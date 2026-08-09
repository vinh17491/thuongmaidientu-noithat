using Microsoft.Extensions.Configuration;
using ThuongMaiDienTu.Services;
using Xunit;

namespace ThuongMaiDienTu.Tests;

public sealed class SeoUrlServiceTests
{
    [Fact] public void ValidHttpsBaseUrlProducesAbsoluteUrl() => Assert.Equal("https://example.test/san-pham/abc", Service("https://example.test/").Build("/san-pham/abc"));
    [Fact] public void HttpLocalBaseUrlIsAccepted() => Assert.Equal("http://localhost:5000/", Service("http://localhost:5000").Build("/"));
    [Fact] public void InvalidSchemeFallsBackToRelative() => Assert.Equal("/sitemap.xml", Service("ftp://example.test").Build("/sitemap.xml"));
    [Fact] public void CredentialsQueryAndFragmentAreRejected() => Assert.False(Service("https://user:pass@example.test/?x=1#f").HasValidBaseUrl);
    [Fact] public void MissingBaseUrlDoesNotCrash() => Assert.Equal("/danh-muc/noi-that", Service(null).Build("/danh-muc/noi-that"));
    [Fact] public void UrlServiceDoesNotAddQueryStrings() => Assert.DoesNotContain("?", Service("https://example.test").Build("/danh-muc/noi-that"));

    private static SeoUrlService Service(string? value) => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Seo:BaseUrl"] = value }).Build());
}
