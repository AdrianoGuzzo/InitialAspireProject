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

    protected async Task<IActionResult> ForwardResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.Headers.RetryAfter is not null)
        {
            Response.Headers.RetryAfter = response.Headers.RetryAfter.Delta.HasValue
                ? ((int)response.Headers.RetryAfter.Delta.Value.TotalSeconds).ToString()
                : response.Headers.RetryAfter.Date?.ToString("R");
        }

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }
}
