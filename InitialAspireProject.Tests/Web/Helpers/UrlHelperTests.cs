using InitialAspireProject.Web.Helpers;

namespace InitialAspireProject.Tests.Web.Helpers;

public class UrlHelperTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/foo")]
    [InlineData("/foo/bar")]
    [InlineData("/foo?bar=1")]
    [InlineData("/foo#section")]
    public void IsLocalUrl_ValidLocalPaths_ReturnsTrue(string url)
    {
        Assert.True(UrlHelper.IsLocalUrl(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://evil.com")]
    [InlineData("http://evil.com")]
    [InlineData("//evil.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://evil.com/path")]
    [InlineData("data:text/html,<script>")]
    public void IsLocalUrl_InvalidUrls_ReturnsFalse(string? url)
    {
        Assert.False(UrlHelper.IsLocalUrl(url));
    }
}
