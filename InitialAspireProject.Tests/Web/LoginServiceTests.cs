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

        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.Equal(expectedToken, result.Token.Token);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsFailure()
    {
        var body = JsonSerializer.Serialize(new { code = "", message = "Invalid credentials" });
        var handler = new StubHttpHandler(HttpStatusCode.Unauthorized, body);
        var service = CreateService(handler);

        var result = await service.LoginAsync("user@test.com", "WrongPassword", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.False(result.IsEmailNotConfirmed);
    }

    [Fact]
    public async Task LoginAsync_EmailNotConfirmed_ReturnsEmailNotConfirmed()
    {
        var body = JsonSerializer.Serialize(new { code = "EmailNotConfirmed", message = "Email not confirmed" });
        var handler = new StubHttpHandler(HttpStatusCode.Unauthorized, body);
        var service = CreateService(handler);

        var result = await service.LoginAsync("user@test.com", "Password123$", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.True(result.IsEmailNotConfirmed);
    }

    [Fact]
    public async Task LoginAsync_ServerError_ReturnsFailure()
    {
        var body = JsonSerializer.Serialize(new { code = "", message = "" });
        var handler = new StubHttpHandler(HttpStatusCode.InternalServerError, body);
        var service = CreateService(handler);

        var result = await service.LoginAsync("user@test.com", "Password123$", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
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
