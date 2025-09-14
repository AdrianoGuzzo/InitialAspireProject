using InitialAspireProject.ApiCore.Domain;
using InitialAspireProject.ApiCore.Service;
using Microsoft.AspNetCore.Mvc;

namespace InitialAspireProject.ApiCore.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(WeatherForecastService weatherForecastService, ILogger<WeatherForecastController> logger) : ControllerBase
{

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
        => weatherForecastService.GetList();
}
