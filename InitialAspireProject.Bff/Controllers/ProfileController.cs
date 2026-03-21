using InitialAspireProject.Bff.Models;
using InitialAspireProject.Bff.Services;
using InitialAspireProject.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InitialAspireProject.Bff.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(IIdentityProxyService identityProxy) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var token = GetBearerToken();
        if (token is null)
            return Unauthorized(new ApiErrorResponse { Code = "Unauthorized", Message = "Bearer token is required." });

        var response = await identityProxy.GetProfileAsync(token);
        return await ForwardResponse(response);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
    {
        var token = GetBearerToken();
        if (token is null)
            return Unauthorized(new ApiErrorResponse { Code = "Unauthorized", Message = "Bearer token is required." });

        var response = await identityProxy.UpdateProfileAsync(model, token);
        return await ForwardResponse(response);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        var token = GetBearerToken();
        if (token is null)
            return Unauthorized(new ApiErrorResponse { Code = "Unauthorized", Message = "Bearer token is required." });

        var response = await identityProxy.ChangePasswordAsync(model, token);
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
