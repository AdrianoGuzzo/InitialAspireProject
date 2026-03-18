using System.Text;
using System.Text.Json;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class WeatherApiServiceTests
{
    private static WeatherApiService CreateService(HttpMessageHandler handler, IHttpContextAccessor accessor)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new WeatherApiService(client, accessor, NullLogger<WeatherApiService>.Instance);
    }

    private static (Mock<IHttpContextAccessor> Accessor, Mock<ISession> Session) SetupSession(string? token)
    {
        var sessionMock = new Mock<ISession>();
        if (token is not null)
        {
            byte[]? bytes = Encoding.UTF8.GetBytes(token);
            sessionMock.Setup(x => x.TryGetValue(SessionConstants.TokenKey, out bytes)).Returns(true);
        }
        else
        {
            byte[]? nullBytes = null;
            sessionMock.Setup(x => x.TryGetValue(It.IsAny<string>(), out nullBytes)).Returns(false);
        }

        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(x => x.Session).Returns(sessionMock.Object);

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(contextMock.Object);

        return (accessorMock, sessionMock);
    }

    private static string MakeForecastsJson(int count)
    {
        var items = Enumerable.Range(1, count)
            .Select(i => new { date = $"2024-01-{i:D2}", temperatureC = 20 + i, summary = "Cool" });
        return JsonSerializer.Serialize(items);
    }

    [Fact]
    public async Task GetWeatherAsync_WithToken_SetsAuthorizationHeader()
    {
        var capturer = new CapturingHandler(HttpStatusCode.OK, "[]");
        var (accessor, _) = SetupSession("my-jwt-token");
        var service = CreateService(capturer, accessor.Object);

        await service.GetWeatherAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Bearer my-jwt-token", capturer.CapturedAuthorization);
    }

    [Fact]
    public async Task GetWeatherAsync_WithoutToken_DoesNotSetAuthorizationHeader()
    {
        var capturer = new CapturingHandler(HttpStatusCode.OK, "[]");
        var (accessor, _) = SetupSession(null);
        var service = CreateService(capturer, accessor.Object);

        await service.GetWeatherAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(capturer.CapturedAuthorization);
    }

    [Fact]
    public async Task GetWeatherAsync_HttpRequestException_ReturnsEmptyArray()
    {
        var handler = new ThrowingHandler();
        var (accessor, _) = SetupSession(null);
        var service = CreateService(handler, accessor.Object);

        var result = await service.GetWeatherAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWeatherAsync_EmptyJsonArray_ReturnsEmptyArray()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "[]");
        var (accessor, _) = SetupSession(null);
        var service = CreateService(handler, accessor.Object);

        var result = await service.GetWeatherAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetWeatherAsync_ValidResponse_ReturnsForecasts()
    {
        var handler = new StubHandler(HttpStatusCode.OK, MakeForecastsJson(3));
        var (accessor, _) = SetupSession(null);
        var service = CreateService(handler, accessor.Object);

        var result = await service.GetWeatherAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Length);
    }

    [Fact]
    public async Task GetWeatherAsync_LimitsToMaxItems()
    {
        var handler = new StubHandler(HttpStatusCode.OK, MakeForecastsJson(10));
        var (accessor, _) = SetupSession(null);
        var service = CreateService(handler, accessor.Object);

        var result = await service.GetWeatherAsync(maxItems: 3, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Length);
    }

    [Fact]
    public async Task GetWeatherAsync_TemperatureFConversion_IsCorrect()
    {
        var json = """[{"date":"2024-01-01","temperatureC":0,"summary":"Cold"}]""";
        var handler = new StubHandler(HttpStatusCode.OK, json);
        var (accessor, _) = SetupSession(null);
        var service = CreateService(handler, accessor.Object);

        var result = await service.GetWeatherAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(32, result[0].TemperatureF);
    }

    [Fact]
    public async Task GetWeatherAsync_NullHttpContext_DoesNotThrow()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "[]");
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        var service = CreateService(handler, accessorMock.Object);

        var result = await service.GetWeatherAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    private sealed class CapturingHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        public string? CapturedAuthorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CapturedAuthorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Network error");
    }
}
