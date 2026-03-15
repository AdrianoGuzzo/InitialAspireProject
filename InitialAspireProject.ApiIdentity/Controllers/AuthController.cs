using System.Security.Claims;
using InitialAspireProject.ApiIdentity.Repository;
using InitialAspireProject.ApiIdentity.Repository.Constants;
using InitialAspireProject.ApiIdentity.Resources;
using InitialAspireProject.ApiIdentity.Services;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace InitialAspireProject.ApiIdentity.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IStringLocalizer<AuthMessages> _localizer;

        public AuthController(UserManager<ApplicationUser> userManager,
                              SignInManager<ApplicationUser> signInManager,
                              RoleManager<IdentityRole> roleManager,
                              TokenService tokenService,
                              IEmailService emailService,
                              IConfiguration configuration,
                              ILogger<AuthController> logger,
                              IStringLocalizer<AuthMessages> localizer)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
            _localizer = localizer;
        }

        [EnableRateLimiting("auth")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new ApplicationUser { Email = model.Email, UserName = model.Email, FullName = model.FullName };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, RoleConstants.User);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = $"{_configuration["App:BaseUrl"]}/confirm-email?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            try
            {
                await _emailService.SendActivationEmailAsync(user.Email!, link);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send activation email to {Email}", user.Email);
            }

            return Ok(_localizer["UserRegisteredCheckEmail"].Value);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized(new LoginErrorResponse { Code = "Unauthorized", Message = _localizer["InvalidCredentials"].Value });

            if (!await _userManager.IsEmailConfirmedAsync(user))
                return Unauthorized(new LoginErrorResponse { Code = "EmailNotConfirmed", Message = _localizer["EmailNotConfirmed"].Value });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (!result.Succeeded) return Unauthorized(_localizer["InvalidCredentials"].Value);

            var roles = await _userManager.GetRolesAsync(user);
            var permissionClaims = await GetPermissionClaimsForRolesAsync(roles);
            var token = _tokenService.CreateToken(user, roles, permissionClaims);

            return Ok(new LoginResponse { Token = token });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.FindByEmailAsync(User?.Identity?.Name ?? "");
            if (user is null) return NotFound(_localizer["UserNotFound"].Value);
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { email = user.Email, fullName = user.FullName, roles });
        }

        [Authorize(Roles = RoleConstants.Admin)]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok(_localizer["AdminOnly"].Value);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("resend-activation")]
        public async Task<IActionResult> ResendActivation([FromBody] ForgotPasswordModel model)
        {
            var genericMessage = _localizer["ResendActivationGenericMessage"].Value;

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || await _userManager.IsEmailConfirmedAsync(user))
                return Ok(genericMessage);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var link = $"{_configuration["App:BaseUrl"]}/confirm-email?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            try
            {
                await _emailService.SendActivationEmailAsync(user.Email!, link);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend activation email to {Email}", user.Email);
            }

            return Ok(genericMessage);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(_localizer["InvalidRequest"].Value);

            var result = await _userManager.ConfirmEmailAsync(user, model.Token);
            if (!result.Succeeded)
                return BadRequest(_localizer["InvalidActivationLink"].Value);

            return Ok(_localizer["EmailConfirmedSuccess"].Value);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            var genericMessage = _localizer["ForgotPasswordGenericMessage"].Value;

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Ok(genericMessage);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var link = $"{_configuration["App:BaseUrl"]}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email!, link);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }

            return Ok(genericMessage);
        }

        [EnableRateLimiting("auth")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(_localizer["InvalidRequest"].Value);

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(_localizer["PasswordResetSuccess"].Value);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin(
            [FromBody] GoogleLoginModel model,
            [FromHeader(Name = "X-Internal-Key")] string? internalKey)
        {
            var expectedKey = _configuration["InternalApiKey"];
            if (string.IsNullOrEmpty(expectedKey) || internalKey != expectedKey)
                return Unauthorized();

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                user = new ApplicationUser { Email = model.Email, UserName = model.Email, FullName = model.Name };
                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create Google SSO user {Email}: {Errors}", model.Email, createResult.Errors);
                    return BadRequest(_localizer["RegistrationFailed"].Value);
                }
                await _userManager.AddToRoleAsync(user, RoleConstants.User);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var permissionClaims = await GetPermissionClaimsForRolesAsync(roles);
            var token = _tokenService.CreateToken(user, roles, permissionClaims);
            return Ok(new LoginResponse { Token = token });
        }

        private async Task<List<Claim>> GetPermissionClaimsForRolesAsync(IList<string> roles)
        {
            var permissions = new HashSet<string>();
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role is null) continue;
                var claims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in claims.Where(c => c.Type == PermissionConstants.ClaimType))
                    permissions.Add(claim.Value);
            }
            return permissions.Select(p => new Claim(PermissionConstants.ClaimType, p)).ToList();
        }
    }
}
