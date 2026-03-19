using InitialAspireProject.ApiCore.Service;
using InitialAspireProject.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InitialAspireProject.ApiCore.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class WeatherForecastController(WeatherForecastService weatherForecastService) : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecastDto>> Get()
    {
        var forecasts = await weatherForecastService.GetListAsync();
        return forecasts.Select(f => new WeatherForecastDto(f.Date, f.TemperatureC, f.Summary));
    }
}
