using InitialAspireProject.Bff.Models;
using InitialAspireProject.Bff.Services;
using InitialAspireProject.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InitialAspireProject.Bff.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IIdentityProxyService identityProxy) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var response = await identityProxy.LoginAsync(model);
        return await ForwardResponse(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var response = await identityProxy.RegisterAsync(model);
        return await ForwardResponse(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest model)
    {
        var response = await identityProxy.RefreshAsync(model);
        return await ForwardResponse(response);
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest model)
    {
        var token = GetBearerToken();
        if (token is null)
            return Unauthorized(new ApiErrorResponse { Code = "Unauthorized", Message = "Bearer token is required." });

        var response = await identityProxy.RevokeAsync(model, token);
        return await ForwardResponse(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
    {
        var response = await identityProxy.ForgotPasswordAsync(model);
        return await ForwardResponse(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
    {
        var response = await identityProxy.ResetPasswordAsync(model);
        return await ForwardResponse(response);
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
    {
        var response = await identityProxy.ConfirmEmailAsync(model);
        return await ForwardResponse(response);
    }

    [HttpPost("resend-activation")]
    public async Task<IActionResult> ResendActivation([FromBody] ForgotPasswordModel model)
    {
        var response = await identityProxy.ResendActivationAsync(model);
        return await ForwardResponse(response);
    }

    private string? GetBearerToken()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        return header["Bearer ".Length..];
    }

    private static async Task<IActionResult> ForwardResponse(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = "application/json"
        };
    }
}
