using System.Text;
using System.Text.Json;
using InitialAspireProject.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace InitialAspireProject.Tests.Web;

public class ConfirmEmailServiceTests
{
    private static ConfirmEmailService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new ConfirmEmailService(httpClient, NullLogger<ConfirmEmailService>.Instance);
    }

    [Fact]
    public async Task ConfirmEmailAsync_Success_ReturnsSuccess()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "\"Email confirmed\"");
        var service = CreateService(handler);

        var result = await service.ConfirmEmailAsync("user@test.com", "valid-token", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ConfirmEmailAsync_InvalidToken_ReturnsFailure()
    {
        var handler = new StubHttpHandler(HttpStatusCode.BadRequest, "\"Invalid activation link\"");
        var service = CreateService(handler);

        var result = await service.ConfirmEmailAsync("user@test.com", "bad-token", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task ConfirmEmailAsync_NetworkFailure_ReturnsConnectionError()
    {
        var handler = new ThrowingHttpHandler();
        var service = CreateService(handler);

        var result = await service.ConfirmEmailAsync("user@test.com", "token", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Erro de conexão", result.Message);
    }

    [Fact]
    public async Task ResendActivationAsync_Success_ReturnsSuccess()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "\"OK\"");
        var service = CreateService(handler);

        var result = await service.ResendActivationAsync("user@test.com", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ResendActivationAsync_NetworkFailure_ReturnsConnectionError()
    {
        var handler = new ThrowingHttpHandler();
        var service = CreateService(handler);

        var result = await service.ResendActivationAsync("user@test.com", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Erro de conexão", result.Message);
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
}
