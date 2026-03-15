using System.Security.Claims;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using InitialAspireProject.ApiIdentity;
using InitialAspireProject.ApiIdentity.Controllers;
using InitialAspireProject.Shared.Models;
using InitialAspireProject.ApiIdentity.Repository;
using InitialAspireProject.ApiIdentity.Resources;
using InitialAspireProject.ApiIdentity.Services;
using InitialAspireProject.Tests.Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InitialAspireProject.Tests.ApiIdentity;

public class AuthControllerTests
{
    private static (Mock<UserManager<ApplicationUser>>, Mock<SignInManager<ApplicationUser>>, AuthController) CreateController()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessorMock = new Mock<IHttpContextAccessor>();
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            userManagerMock.Object,
            contextAccessorMock.Object,
            claimsFactoryMock.Object,
            null!, null!, null!, null!);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super-secret-test-key-32-chars-ok!!",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["App:BaseUrl"] = "https://localhost:5001"
            })
            .Build();

        var tokenService = new TokenService(config);
        var localizer = new Mock<IStringLocalizer<AuthMessages>>();
        localizer.Setup(l => l[It.IsAny<string>()])
                 .Returns<string>(key => new LocalizedString(key, key));

        var controller = new AuthController(
            userManagerMock.Object,
            signInManagerMock.Object,
            tokenService,
            Mock.Of<IEmailService>(),
            config,
            NullLogger<AuthController>.Instance,
            localizer.Object);

        return (userManagerMock, signInManagerMock, controller);
    }

    // --- Register ---

    private static void SetupRegisterMocks(Mock<UserManager<ApplicationUser>> userManagerMock)
    {
        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("confirmation-token");
    }

    private static void SetupLoginMocks(Mock<UserManager<ApplicationUser>> userManagerMock, Mock<SignInManager<ApplicationUser>> signInManagerMock, ApplicationUser user, string password)
    {
        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, password, true))
            .ReturnsAsync(IdentitySignInResult.Success);
        userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
    }

    [Fact]
    public async Task Register_ValidModel_ReturnsOk()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new RegisterModelBuilder().Build();

        SetupRegisterMocks(userManagerMock);

        var result = await controller.Register(model);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Register_IdentityFailure_ReturnsBadRequest()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new RegisterModelBuilder().Build();

        var errors = new[] { new IdentityError { Code = "DuplicateEmail", Description = "Email já está em uso." } };
        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        var result = await controller.Register(model);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Register_CreatesUserWithCorrectEmail()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new RegisterModelBuilder()
            .WithEmail("jane@test.com")
            .Build();

        ApplicationUser? createdUser = null;
        SetupRegisterMocks(userManagerMock);
        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);

        await controller.Register(model);

        Assert.NotNull(createdUser);
        Assert.Equal("jane@test.com", createdUser.Email);
    }

    [Fact]
    public async Task Register_Success_AssignsUserRole()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new RegisterModelBuilder().Build();

        SetupRegisterMocks(userManagerMock);

        await controller.Register(model);

        userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Once);
    }

    // --- Login ---

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        var (userManagerMock, signInManagerMock, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();
        var model = new LoginModelBuilder().WithEmail(user.Email!).WithPassword("Password123$").Build();

        SetupLoginMocks(userManagerMock, signInManagerMock, user, model.Password);

        var result = await controller.Login(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Login_UserNotFound_ReturnsUnauthorized()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new LoginModelBuilder().Build();

        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await controller.Login(model);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var (userManagerMock, signInManagerMock, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();
        var model = new LoginModelBuilder().WithEmail(user.Email!).WithPassword("WrongPass").Build();

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(IdentitySignInResult.Failed);

        var result = await controller.Login(model);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsTokenInResponse()
    {
        var (userManagerMock, signInManagerMock, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();
        var model = new LoginModelBuilder().WithEmail(user.Email!).WithPassword("Password123$").Build();

        SetupLoginMocks(userManagerMock, signInManagerMock, user, model.Password);
        userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin", "User" });

        var result = await controller.Login(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        var token = ok.Value!.GetType().GetProperty("Token")?.GetValue(ok.Value) as string;
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    // --- Profile ---

    [Fact]
    public async Task Profile_ExistingUser_ReturnsUserData()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Email!) }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var result = await controller.Profile();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Profile_UserNotFound_ReturnsNotFound()
    {
        var (userManagerMock, _, controller) = CreateController();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await controller.Profile();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // --- Login: Email Not Confirmed ---

    [Fact]
    public async Task Login_EmailNotConfirmed_ReturnsUnauthorized()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();
        var model = new LoginModelBuilder().WithEmail(user.Email!).WithPassword("Password123$").Build();

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(false);

        var result = await controller.Login(model);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var value = unauthorized.Value;
        var code = value!.GetType().GetProperty("Code")?.GetValue(value) as string;
        Assert.Equal("EmailNotConfirmed", code);
    }

    // --- Register: Sends Activation Email ---

    [Fact]
    public async Task Register_Success_SendsActivationEmail()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new RegisterModelBuilder().Build();

        SetupRegisterMocks(userManagerMock);

        var result = await controller.Register(model);

        Assert.IsType<OkObjectResult>(result);
        var ok = (OkObjectResult)result;
        Assert.Equal("UserRegisteredCheckEmail", ok.Value);
    }

    [Fact]
    public async Task Register_Success_GeneratesEmailConfirmationToken()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new RegisterModelBuilder().Build();

        SetupRegisterMocks(userManagerMock);

        await controller.Register(model);

        userManagerMock.Verify(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    // --- ConfirmEmail ---

    [Fact]
    public async Task ConfirmEmail_ValidToken_ReturnsOk()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();
        var model = new ConfirmEmailModel { Email = user.Email!, Token = "valid-token" };

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.ConfirmEmailAsync(user, "valid-token")).ReturnsAsync(IdentityResult.Success);

        var result = await controller.ConfirmEmail(model);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmEmail_InvalidToken_ReturnsBadRequest()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();
        var model = new ConfirmEmailModel { Email = user.Email!, Token = "invalid-token" };

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.ConfirmEmailAsync(user, "invalid-token"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Invalid token" }));

        var result = await controller.ConfirmEmail(model);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmEmail_UserNotFound_ReturnsBadRequest()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new ConfirmEmailModel { Email = "notfound@test.com", Token = "token" };

        userManagerMock.Setup(x => x.FindByEmailAsync("notfound@test.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await controller.ConfirmEmail(model);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // --- ResendActivation ---

    [Fact]
    public async Task ResendActivation_UnconfirmedUser_ReturnsOk()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();
        var model = new ForgotPasswordModel { Email = user.Email! };

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(false);
        userManagerMock.Setup(x => x.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("token");

        var result = await controller.ResendActivation(model);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResendActivation_AlreadyConfirmed_ReturnsOkGenericMessage()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();
        var model = new ForgotPasswordModel { Email = user.Email! };

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);

        var result = await controller.ResendActivation(model);

        Assert.IsType<OkObjectResult>(result);
        userManagerMock.Verify(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ResendActivation_UserNotFound_ReturnsOkGenericMessage()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new ForgotPasswordModel { Email = "notfound@test.com" };

        userManagerMock.Setup(x => x.FindByEmailAsync("notfound@test.com")).ReturnsAsync((ApplicationUser?)null);

        var result = await controller.ResendActivation(model);

        Assert.IsType<OkObjectResult>(result);
    }

    // --- UpdateProfile ---

    [Fact]
    public async Task UpdateProfile_ValidModel_ReturnsOk()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Email!) }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var model = new UpdateProfileModel { FullName = "New Name" };
        var result = await controller.UpdateProfile(model);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("New Name", user.FullName);
    }

    [Fact]
    public async Task UpdateProfile_UserNotFound_ReturnsNotFound()
    {
        var (userManagerMock, _, controller) = CreateController();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var model = new UpdateProfileModel { FullName = "New Name" };
        var result = await controller.UpdateProfile(model);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProfile_UpdateFails_ReturnsBadRequest()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Email!) }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Update failed" }));

        var model = new UpdateProfileModel { FullName = "New Name" };
        var result = await controller.UpdateProfile(model);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // --- ChangePassword ---

    [Fact]
    public async Task ChangePassword_ValidModel_ReturnsOk()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Email!) }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.ChangePasswordAsync(user, "OldPass123$", "NewPass123$"))
            .ReturnsAsync(IdentityResult.Success);

        var model = new ChangePasswordModel { CurrentPassword = "OldPass123$", NewPassword = "NewPass123$" };
        var result = await controller.ChangePassword(model);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Email!) }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.ChangePasswordAsync(user, "WrongPass", "NewPass123$"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordMismatch", Description = "Incorrect password." }));

        var model = new ChangePasswordModel { CurrentPassword = "WrongPass", NewPassword = "NewPass123$" };
        var result = await controller.ChangePassword(model);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_ReturnsNotFound()
    {
        var (userManagerMock, _, controller) = CreateController();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var model = new ChangePasswordModel { CurrentPassword = "OldPass", NewPassword = "NewPass123$" };
        var result = await controller.ChangePassword(model);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // --- Profile returns ProfileResponse ---

    [Fact]
    public async Task Profile_ReturnsProfileResponse()
    {
        var (userManagerMock, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("test@test.com").Build();

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, user.Email!) }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        var result = await controller.Profile();

        var ok = Assert.IsType<OkObjectResult>(result);
        var profile = Assert.IsType<ProfileResponse>(ok.Value);
        Assert.Equal(user.Email, profile.Email);
        Assert.Equal(user.FullName, profile.FullName);
        Assert.Contains("User", profile.Roles);
    }

    // --- AdminOnly ---

    [Fact]
    public void AdminOnly_ReturnsOk()
    {
        var (_, _, controller) = CreateController();

        var result = controller.AdminOnly();

        Assert.IsType<OkObjectResult>(result);
    }
}
