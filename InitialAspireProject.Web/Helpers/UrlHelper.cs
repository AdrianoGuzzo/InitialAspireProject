namespace InitialAspireProject.Web.Helpers;

public static class UrlHelper
{
    public static bool IsLocalUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        if (!url.StartsWith('/'))
            return false;

        if (url.StartsWith("//"))
            return false;

        if (url.Contains("://"))
            return false;

        return true;
    }
}
