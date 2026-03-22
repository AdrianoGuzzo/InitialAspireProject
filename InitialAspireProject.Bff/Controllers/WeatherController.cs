using InitialAspireProject.Bff.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InitialAspireProject.Bff.Controllers;

[ApiController]
[Route("api/weather")]
[Authorize]
public class WeatherController(ICoreProxyService coreProxy) : BffControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWeather()
    {
        var response = await coreProxy.GetWeatherAsync(GetRequiredBearerToken(), GetAcceptLanguage());
        return await ForwardResponse(response);
    }
}
