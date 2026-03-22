using Microsoft.AspNetCore.Mvc;

namespace InitialAspireProject.Bff.Controllers;

public abstract class BffControllerBase : ControllerBase
{
    protected string? GetBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        return header["Bearer ".Length..];
    }

    protected string GetRequiredBearerToken()
    {
        return GetBearerToken()!;
    }

    protected string? GetAcceptLanguage()
    {
        return Request.Headers.AcceptLanguage.ToString() is { Length: > 0 } lang ? lang : null;
    }

    protected static async Task<IActionResult> ForwardResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }
}
