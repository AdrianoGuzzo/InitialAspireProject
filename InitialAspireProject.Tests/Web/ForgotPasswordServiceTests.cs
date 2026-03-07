using System.Text;
using System.Text.Json;
using InitialAspireProject.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace InitialAspireProject.Tests.Web;

public class ForgotPasswordServiceTests
{
    private static ForgotPasswordService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new ForgotPasswordService(httpClient, NullLogger<ForgotPasswordService>.Instance);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ApiReturns200_ReturnsSuccess()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "\"ok\"");
        var service = CreateService(handler);

        var result = await service.ForgotPasswordAsync("user@test.com", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ApiReturns404_StillReturnsSuccess()
    {
        var handler = new StubHttpHandler(HttpStatusCode.NotFound, "\"not found\"");
        var service = CreateService(handler);

        var result = await service.ForgotPasswordAsync("notfound@test.com", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ForgotPasswordAsync_ApiReturns500_StillReturnsSuccess()
    {
        var handler = new StubHttpHandler(HttpStatusCode.InternalServerError, "\"error\"");
        var service = CreateService(handler);

        var result = await service.ForgotPasswordAsync("user@test.com", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ForgotPasswordAsync_NetworkFailure_ReturnsFailure()
    {
        var handler = new ThrowingHttpHandler();
        var service = CreateService(handler);

        var result = await service.ForgotPasswordAsync("user@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ForgotPasswordAsync_SendsCorrectEmailInBody()
    {
        var handler = new CapturingHandler();
        var service = CreateService(handler);

        await service.ForgotPasswordAsync("target@test.com", TestContext.Current.CancellationToken);

        Assert.NotNull(handler.CapturedBody);
        Assert.Contains("target@test.com", handler.CapturedBody);
    }

    private sealed class StubHttpHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    private sealed class ThrowingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Network error");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? CapturedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CapturedBody = request.Content is not null
                ? await request.Content.ReadAsStringAsync(ct)
                : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("\"ok\"", Encoding.UTF8, "application/json")
            };
        }
    }
}
