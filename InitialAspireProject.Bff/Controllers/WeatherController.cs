using InitialAspireProject.Bff.Models;
using InitialAspireProject.Bff.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InitialAspireProject.Bff.Controllers;

[ApiController]
[Route("api/weather")]
[Authorize]
public class WeatherController(ICoreProxyService coreProxy) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWeather()
    {
        var token = GetBearerToken();
        if (token is null)
            return Unauthorized(new ApiErrorResponse { Code = "Unauthorized", Message = "Bearer token is required." });

        var response = await coreProxy.GetWeatherAsync(token);
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }

    private string? GetBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        return header["Bearer ".Length..];
    }
}
