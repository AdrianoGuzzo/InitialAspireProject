using InitialAspireProject.ApiCore.Domain;
using InitialAspireProject.ApiCore.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InitialAspireProject.ApiCore.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class WeatherForecastController(WeatherForecastService weatherForecastService) : ControllerBase
{
    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
        => await weatherForecastService.GetListAsync();
}
