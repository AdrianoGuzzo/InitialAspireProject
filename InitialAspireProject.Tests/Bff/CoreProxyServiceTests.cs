using System.Net;
using System.Text;
using InitialAspireProject.Bff.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace InitialAspireProject.Tests.Bff;

public class CoreProxyServiceTests
{
    private static (CoreProxyService Service, CapturingHandler Handler) CreateService()
    {
        var handler = new CapturingHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var service = new CoreProxyService(client, NullLogger<CoreProxyService>.Instance);
        return (service, handler);
    }

    [Fact]
    public async Task GetWeatherAsync_UsesGetMethod()
    {
        var (service, handler) = CreateService();
        await service.GetWeatherAsync("my-jwt");

        Assert.Equal(HttpMethod.Get, handler.CapturedMethod);
    }

    [Fact]
    public async Task GetWeatherAsync_TargetsCorrectUrl()
    {
        var (service, handler) = CreateService();
        await service.GetWeatherAsync("my-jwt");

        Assert.Equal("/WeatherForecast", handler.CapturedUrl);
    }

    [Fact]
    public async Task GetWeatherAsync_IncludesBearerToken()
    {
        var (service, handler) = CreateService();
        await service.GetWeatherAsync("my-jwt");

        Assert.Equal("Bearer my-jwt", handler.CapturedAuthorization);
    }

    [Fact]
    public async Task GetWeatherAsync_ReturnsHttpResponse()
    {
        var (service, _) = CreateService();
        var response = await service.GetWeatherAsync("my-jwt");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    internal sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpMethod? CapturedMethod { get; private set; }
        public string? CapturedUrl { get; private set; }
        public string? CapturedAuthorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CapturedMethod = request.Method;
            CapturedUrl = request.RequestUri?.AbsolutePath;
            CapturedAuthorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        }
    }
}
