using InitialAspireProject.Bff.Services;
using InitialAspireProject.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InitialAspireProject.Bff.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IIdentityProxyService identityProxy) : BffControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var response = await identityProxy.LoginAsync(model, GetAcceptLanguage());
        return await ForwardResponse(response);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var response = await identityProxy.RegisterAsync(model, GetAcceptLanguage());
        return await ForwardResponse(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest model)
    {
        var response = await identityProxy.RefreshAsync(model, GetAcceptLanguage());
        return await ForwardResponse(response);
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenRequest model)
    {
        var response = await identityProxy.RevokeAsync(model, GetRequiredBearerToken(), GetAcceptLanguage());
        return await ForwardResponse(response);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
    {
        var response = await identityProxy.ForgotPasswordAsync(model, GetAcceptLanguage());
        return await ForwardResponse(response);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
    {
        var response = await identityProxy.ResetPasswordAsync(model, GetAcceptLanguage());
        return await ForwardResponse(response);
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
    {
        var response = await identityProxy.ConfirmEmailAsync(model, GetAcceptLanguage());
        return await ForwardResponse(response);
    }

    [HttpPost("resend-activation")]
    public async Task<IActionResult> ResendActivation([FromBody] ForgotPasswordModel model)
    {
        var response = await identityProxy.ResendActivationAsync(model, GetAcceptLanguage());
        return await ForwardResponse(response);
    }
}
