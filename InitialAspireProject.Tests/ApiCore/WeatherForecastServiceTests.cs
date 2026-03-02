using InitialAspireProject.ApiCore;
using InitialAspireProject.ApiCore.Service;
using InitialAspireProject.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace InitialAspireProject.Tests.ApiCore;

public class WeatherForecastServiceTests
{
    private static CoreDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CoreDbContext(options);
    }

    [Fact]
    public async Task GetListAsync_EmptyDb_ReturnsEmptyList()
    {
        await using var context = CreateInMemoryContext();
        var service = new WeatherForecastService(context);

        var result = await service.GetListAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetListAsync_WithData_ReturnsMappedDomainObjects()
    {
        await using var context = CreateInMemoryContext();
        var service = new WeatherForecastService(context);

        var forecasts = WeatherForecastBuilder.BuildList(3);
        await service.SaveRangeAsync(forecasts);

        var result = (await service.GetListAsync()).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetListAsync_MapsDateCorrectly()
    {
        await using var context = CreateInMemoryContext();
        var service = new WeatherForecastService(context);

        var expectedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
        var forecast = new WeatherForecastBuilder().WithDate(expectedDate).Build();
        await service.SaveRangeAsync([forecast]);

        var result = (await service.GetListAsync()).Single();

        Assert.Equal(expectedDate, result.Date);
    }

    [Fact]
    public async Task GetListAsync_MapsTemperatureCorrectly()
    {
        await using var context = CreateInMemoryContext();
        var service = new WeatherForecastService(context);

        var forecast = new WeatherForecastBuilder().WithTemperatureC(42).Build();
        await service.SaveRangeAsync([forecast]);

        var result = (await service.GetListAsync()).Single();

        Assert.Equal(42, result.TemperatureC);
    }

    [Fact]
    public async Task GetListAsync_MapsSummaryCorrectly()
    {
        await using var context = CreateInMemoryContext();
        var service = new WeatherForecastService(context);

        var forecast = new WeatherForecastBuilder().WithSummary("Scorching").Build();
        await service.SaveRangeAsync([forecast]);

        var result = (await service.GetListAsync()).Single();

        Assert.Equal("Scorching", result.Summary);
    }

    [Fact]
    public async Task SaveRangeAsync_PersistsCorrectCount()
    {
        await using var context = CreateInMemoryContext();
        var service = new WeatherForecastService(context);

        var forecasts = WeatherForecastBuilder.BuildList(5);
        await service.SaveRangeAsync(forecasts);

        Assert.Equal(5, await context.WeatherForecast.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveRangeAsync_MultipleCallsAccumulate()
    {
        await using var context = CreateInMemoryContext();
        var service = new WeatherForecastService(context);

        await service.SaveRangeAsync(WeatherForecastBuilder.BuildList(2));
        await service.SaveRangeAsync(WeatherForecastBuilder.BuildList(3));

        Assert.Equal(5, await context.WeatherForecast.CountAsync(TestContext.Current.CancellationToken));
    }
}
