using System.Text;
using System.Text.Json;
using InitialAspireProject.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace InitialAspireProject.Tests.Web;

public class ResetPasswordServiceTests
{
    private static ResetPasswordService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new ResetPasswordService(httpClient, NullLogger<ResetPasswordService>.Instance);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidData_ReturnsSuccess()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, "\"Password reset successfully.\"");
        var service = CreateService(handler);

        var result = await service.ResetPasswordAsync("user@test.com", "token", "NewPass123$", "NewPass123$", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ResetPasswordAsync_BadRequest_ReturnsErrorMessages()
    {
        var errors = JsonSerializer.Serialize(new[]
        {
            new { Code = "InvalidToken", Description = "Token inválido." }
        });
        var handler = new StubHttpHandler(HttpStatusCode.BadRequest, errors);
        var service = CreateService(handler);

        var result = await service.ResetPasswordAsync("user@test.com", "bad-token", "NewPass123$", "NewPass123$", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Token inválido.", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_MultipleErrors_CombinesWithNewline()
    {
        var errors = JsonSerializer.Serialize(new[]
        {
            new { Code = "PasswordTooShort", Description = "Senha muito curta." },
            new { Code = "PasswordRequiresUpper", Description = "Senha requer maiúscula." }
        });
        var handler = new StubHttpHandler(HttpStatusCode.BadRequest, errors);
        var service = CreateService(handler);

        var result = await service.ResetPasswordAsync("user@test.com", "token", "short", "short", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("Senha muito curta.", result.Message);
        Assert.Contains("Senha requer maiúscula.", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_NetworkFailure_ReturnsInternalError()
    {
        var handler = new ThrowingHttpHandler();
        var service = CreateService(handler);

        var result = await service.ResetPasswordAsync("user@test.com", "token", "NewPass123$", "NewPass123$", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Erro de conexão. Tente novamente mais tarde.", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_SendsCorrectFieldsInBody()
    {
        var handler = new CapturingHandler();
        var service = CreateService(handler);

        await service.ResetPasswordAsync("user@test.com", "my-token", "NewPass123$", "NewPass123$", TestContext.Current.CancellationToken);

        Assert.NotNull(handler.CapturedBody);
        Assert.Contains("user@test.com", handler.CapturedBody);
        Assert.Contains("my-token", handler.CapturedBody);
        Assert.Contains("NewPass123$", handler.CapturedBody);
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
                Content = new StringContent("\"Password reset successfully.\"", Encoding.UTF8, "application/json")
            };
        }
    }
}
