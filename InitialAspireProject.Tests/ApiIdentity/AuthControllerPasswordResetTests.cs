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

public class AuthControllerPasswordResetTests
{
    private static (Mock<UserManager<ApplicationUser>>, Mock<SignInManager<ApplicationUser>>, Mock<IEmailService>, AuthController) CreateController(Mock<IEmailService>? emailMock = null)
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
        emailMock ??= new Mock<IEmailService>();

        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        var roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        var localizer = new Mock<IStringLocalizer<AuthMessages>>();
        localizer.Setup(l => l[It.IsAny<string>()])
                 .Returns<string>(key => new LocalizedString(key, key));

        var controller = new AuthController(
            userManagerMock.Object,
            signInManagerMock.Object,
            roleManagerMock.Object,
            tokenService,
            emailMock.Object,
            config,
            NullLogger<AuthController>.Instance,
            localizer.Object);

        return (userManagerMock, signInManagerMock, emailMock, controller);
    }

    // --- ForgotPassword ---

    [Fact]
    public async Task ForgotPassword_UserNotFound_ReturnsOk()
    {
        var (userManagerMock, _, _, controller) = CreateController();
        var model = new ForgotPasswordModelBuilder().Build();

        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await controller.ForgotPassword(model);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ForgotPassword_UserFound_GeneratesToken()
    {
        var (userManagerMock, _, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("user@test.com").Build();
        var model = new ForgotPasswordModelBuilder().WithEmail("user@test.com").Build();

        userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

        await controller.ForgotPassword(model);

        userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_UserFound_SendsEmail()
    {
        var emailMock = new Mock<IEmailService>();
        var (userManagerMock, _, _, controller) = CreateController(emailMock);
        var user = new ApplicationUserBuilder().WithEmail("user@test.com").Build();
        var model = new ForgotPasswordModelBuilder().WithEmail("user@test.com").Build();

        userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        emailMock.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await controller.ForgotPassword(model);

        emailMock.Verify(x => x.SendPasswordResetEmailAsync("user@test.com", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ResetLinkContainsEmailAndToken()
    {
        var emailMock = new Mock<IEmailService>();
        var (userManagerMock, _, _, controller) = CreateController(emailMock);
        var user = new ApplicationUserBuilder().WithEmail("user@test.com").Build();
        var model = new ForgotPasswordModelBuilder().WithEmail("user@test.com").Build();

        userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token-123");

        string? capturedLink = null;
        emailMock.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, link, _) => capturedLink = link)
            .Returns(Task.CompletedTask);

        await controller.ForgotPassword(model);

        Assert.NotNull(capturedLink);
        Assert.Contains("email=", capturedLink);
        Assert.Contains("token=", capturedLink);
    }

    [Fact]
    public async Task ForgotPassword_UserFound_ReturnsOkWithGenericMessage()
    {
        var emailMock = new Mock<IEmailService>();
        var (userManagerMock, _, _, controller) = CreateController(emailMock);
        var user = new ApplicationUserBuilder().WithEmail("user@test.com").Build();
        var model = new ForgotPasswordModelBuilder().WithEmail("user@test.com").Build();

        userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        emailMock.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var (userManagerMock2, _, _, controller2) = CreateController();
        userManagerMock2.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        var modelNotFound = new ForgotPasswordModelBuilder().Build();

        var resultFound = await controller.ForgotPassword(model);
        var resultNotFound = await controller2.ForgotPassword(modelNotFound);

        var okFound = Assert.IsType<OkObjectResult>(resultFound);
        var okNotFound = Assert.IsType<OkObjectResult>(resultNotFound);
        Assert.Equal(okFound.Value, okNotFound.Value);
    }

    [Fact]
    public async Task ForgotPassword_EmailServiceThrows_StillReturnsOk()
    {
        var emailMock = new Mock<IEmailService>();
        var (userManagerMock, _, _, controller) = CreateController(emailMock);
        var user = new ApplicationUserBuilder().WithEmail("user@test.com").Build();
        var model = new ForgotPasswordModelBuilder().WithEmail("user@test.com").Build();

        userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        emailMock.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP failure"));

        var result = await controller.ForgotPassword(model);

        Assert.IsType<OkObjectResult>(result);
    }

    // --- ResetPassword ---

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsOk()
    {
        var (userManagerMock, _, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("user@test.com").Build();
        var model = new ResetPasswordModelBuilder().WithEmail("user@test.com").Build();

        userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await controller.ResetPassword(model);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_UserNotFound_ReturnsBadRequest()
    {
        var (userManagerMock, _, _, controller) = CreateController();
        var model = new ResetPasswordModelBuilder().Build();

        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await controller.ResetPassword(model);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        var (userManagerMock, _, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("user@test.com").Build();
        var model = new ResetPasswordModelBuilder().WithEmail("user@test.com").Build();

        userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Invalid token." }));

        var result = await controller.ResetPassword(model);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_IdentityErrors_ReturnsBadRequestWithDescriptions()
    {
        var (userManagerMock, _, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("user@test.com").Build();
        var model = new ResetPasswordModelBuilder().WithEmail("user@test.com").Build();
        var errors = new[] { new IdentityError { Code = "PasswordTooShort", Description = "Password too short." } };

        userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        var result = await controller.ResetPassword(model);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task ResetPassword_CallsResetPasswordAsyncWithCorrectArgs()
    {
        var (userManagerMock, _, _, controller) = CreateController();
        var user = new ApplicationUserBuilder().WithEmail("user@test.com").Build();
        var model = new ResetPasswordModelBuilder()
            .WithEmail("user@test.com")
            .WithToken("specific-token")
            .WithNewPassword("NewPass123$")
            .Build();

        userManagerMock.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
        userManagerMock.Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        await controller.ResetPassword(model);

        userManagerMock.Verify(x => x.ResetPasswordAsync(user, "specific-token", "NewPass123$"), Times.Once);
    }
}
