using InitialAspireProject.Bff.Services;
using InitialAspireProject.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InitialAspireProject.Bff.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(IIdentityProxyService identityProxy) : BffControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var response = await identityProxy.GetProfileAsync(GetRequiredBearerToken(), GetAcceptLanguage());
        return await ForwardResponse(response);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
    {
        var response = await identityProxy.UpdateProfileAsync(model, GetRequiredBearerToken(), GetAcceptLanguage());
        return await ForwardResponse(response);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
    {
        var response = await identityProxy.ChangePasswordAsync(model, GetRequiredBearerToken(), GetAcceptLanguage());
        return await ForwardResponse(response);
    }
}
