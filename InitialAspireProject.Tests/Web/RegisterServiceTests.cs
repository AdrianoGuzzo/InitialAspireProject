using System.Text;
using System.Text.Json;
using InitialAspireProject.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace InitialAspireProject.Tests.Web;

public class RegisterServiceTests
{
    private static RegisterService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new RegisterService(httpClient, NullLogger<RegisterService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsSuccess()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "\"User registered\"");
        var service = CreateService(handler);

        var result = await service.RegisterAsync("John Doe", "john@example.com", "Password123$", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Null(result.Message);
    }

    [Fact]
    public async Task RegisterAsync_IdentityErrors_ReturnsErrorMessages()
    {
        var errors = JsonSerializer.Serialize(new[]
        {
            new { Code = "PasswordTooShort", Description = "A senha deve ter pelo menos 8 caracteres." }
        });
        var handler = new StubHttpHandler(HttpStatusCode.BadRequest, errors);
        var service = CreateService(handler);

        var result = await service.RegisterAsync("John", "john@example.com", "short", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("A senha deve ter pelo menos 8 caracteres.", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_MultipleIdentityErrors_CombinesMessages()
    {
        var errors = JsonSerializer.Serialize(new[]
        {
            new { Code = "PasswordTooShort", Description = "Senha muito curta." },
            new { Code = "PasswordRequiresUpper", Description = "Senha requer letra maiúscula." }
        });
        var handler = new StubHttpHandler(HttpStatusCode.BadRequest, errors);
        var service = CreateService(handler);

        var result = await service.RegisterAsync("John", "john@example.com", "short", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Senha muito curta.", result.Message);
        Assert.Contains("Senha requer letra maiúscula.", result.Message);
    }

    [Fact]
    public async Task RegisterAsync_NetworkFailure_ReturnsInternalError()
    {
        var handler = new ThrowingHttpHandler();
        var service = CreateService(handler);

        var result = await service.RegisterAsync("John", "john@example.com", "Password123$", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Erro interno do servidor. Tente novamente mais tarde.", result.Message);
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
