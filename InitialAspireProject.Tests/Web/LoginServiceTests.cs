using System.Text;
using System.Text.Json;
using InitialAspireProject.Web.Services;

namespace InitialAspireProject.Tests.Web;

public class LoginServiceTests
{
    private static LoginService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new LoginService(httpClient);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test";
        var body = JsonSerializer.Serialize(new { token = expectedToken });
        var handler = new StubHttpHandler(HttpStatusCode.OK, body);
        var service = CreateService(handler);

        var result = await service.LoginAsync("user@test.com", "Password123$", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(expectedToken, result.Token);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsNull()
    {
        var handler = new StubHttpHandler(HttpStatusCode.Unauthorized, "");
        var service = CreateService(handler);

        var result = await service.LoginAsync("user@test.com", "WrongPassword", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ServerError_ReturnsNull()
    {
        var handler = new StubHttpHandler(HttpStatusCode.InternalServerError, "");
        var service = CreateService(handler);

        var result = await service.LoginAsync("user@test.com", "Password123$", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_NetworkFailure_ThrowsHttpRequestException()
    {
        var handler = new ThrowingHttpHandler();
        var service = CreateService(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.LoginAsync("user@test.com", "Password123$", TestContext.Current.CancellationToken));
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
