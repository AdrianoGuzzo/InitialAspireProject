using System.Security.Claims;
using InitialAspireProject.ApiIdentity.Resources;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace InitialAspireProject.ApiIdentity.Controllers
{
    [ApiController]
    [Route("permissions")]
    [Authorize(Policy = PermissionConstants.CanManagePermissions)]
    public class PermissionController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IStringLocalizer<AuthMessages> _localizer;

        public PermissionController(RoleManager<IdentityRole> roleManager, IStringLocalizer<AuthMessages> localizer)
        {
            _roleManager = roleManager;
            _localizer = localizer;
        }

        [HttpGet]
        public IActionResult GetAllPermissions()
        {
            return Ok(PermissionConstants.All);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRolePermissions()
        {
            var result = new List<RolePermissionsDto>();
            foreach (var role in _roleManager.Roles.ToList())
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                result.Add(new RolePermissionsDto
                {
                    RoleName = role.Name!,
                    Permissions = claims
                        .Where(c => c.Type == PermissionConstants.ClaimType)
                        .Select(c => c.Value)
                        .ToList()
                });
            }
            return Ok(result);
        }

        [HttpGet("roles/{roleName}")]
        public async Task<IActionResult> GetRolePermissions(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
                return NotFound(_localizer["RoleNotFound"].Value);

            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == PermissionConstants.ClaimType)
                .Select(c => c.Value)
                .ToList();

            return Ok(new RolePermissionsDto { RoleName = role.Name!, Permissions = permissions });
        }

        [HttpPost("roles/{roleName}")]
        public async Task<IActionResult> AssignPermission(string roleName, [FromBody] AssignPermissionModel model)
        {
            if (!PermissionConstants.All.Contains(model.Permission))
                return BadRequest(_localizer["InvalidPermission"].Value);

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
                return NotFound(_localizer["RoleNotFound"].Value);

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            if (existingClaims.Any(c => c.Type == PermissionConstants.ClaimType && c.Value == model.Permission))
                return Conflict(_localizer["PermissionAlreadyAssigned"].Value);

            await _roleManager.AddClaimAsync(role, new Claim(PermissionConstants.ClaimType, model.Permission));
            return Ok(_localizer["PermissionAssigned"].Value);
        }

        [HttpDelete("roles/{roleName}/{permission}")]
        public async Task<IActionResult> RemovePermission(string roleName, string permission)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
                return NotFound(_localizer["RoleNotFound"].Value);

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var claimToRemove = existingClaims.FirstOrDefault(c => c.Type == PermissionConstants.ClaimType && c.Value == permission);
            if (claimToRemove is null)
                return NotFound(_localizer["InvalidPermission"].Value);

            await _roleManager.RemoveClaimAsync(role, claimToRemove);
            return Ok(_localizer["PermissionRemoved"].Value);
        }
    }
}
