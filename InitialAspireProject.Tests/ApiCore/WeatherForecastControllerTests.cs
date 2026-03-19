using InitialAspireProject.ApiCore;
using InitialAspireProject.ApiCore.Controllers;
using InitialAspireProject.ApiCore.Service;
using InitialAspireProject.Shared.Models;
using InitialAspireProject.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace InitialAspireProject.Tests.ApiCore;

public class WeatherForecastControllerTests
{
    private static (CoreDbContext, WeatherForecastController) CreateController()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new CoreDbContext(options);
        var service = new WeatherForecastService(context);
        var controller = new WeatherForecastController(service);
        return (context, controller);
    }

    [Fact]
    public async Task Get_EmptyDb_ReturnsEmptyEnumerable()
    {
        var (context, controller) = CreateController();
        await using var _ = context;

        var result = await controller.Get();

        Assert.Empty(result);
    }

    [Fact]
    public async Task Get_WithData_ReturnsAllForecasts()
    {
        var (context, controller) = CreateController();
        await using var _ = context;

        var service = new WeatherForecastService(context);
        await service.SaveRangeAsync(WeatherForecastBuilder.BuildList(4));

        var result = (await controller.Get()).ToList();

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task Get_ReturnsDomainObjects()
    {
        var (context, controller) = CreateController();
        await using var _ = context;

        var service = new WeatherForecastService(context);
        var forecast = new WeatherForecastBuilder().WithTemperatureC(30).WithSummary("Hot").Build();
        await service.SaveRangeAsync([forecast]);

        var result = (await controller.Get()).Single();

        Assert.IsType<WeatherForecastDto>(result);
        Assert.Equal(30, result.TemperatureC);
        Assert.Equal("Hot", result.Summary);
    }
}
