using System.Security.Claims;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using InitialAspireProject.ApiIdentity;
using InitialAspireProject.ApiIdentity.Controllers;
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

    [Fact]
    public async Task Register_ValidModel_ReturnsOk()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new RegisterModelBuilder().Build();

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

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
    public async Task Register_CreatesUserWithCorrectEmailAndFullName()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new RegisterModelBuilder()
            .WithEmail("jane@test.com")
            .WithFullName("Jane Doe")
            .Build();

        ApplicationUser? createdUser = null;
        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, _) => createdUser = user)
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        await controller.Register(model);

        Assert.NotNull(createdUser);
        Assert.Equal("jane@test.com", createdUser.Email);
        Assert.Equal("Jane Doe", createdUser.FullName);
    }

    [Fact]
    public async Task Register_Success_AssignsUserRole()
    {
        var (userManagerMock, _, controller) = CreateController();
        var model = new RegisterModelBuilder().Build();

        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

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

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, model.Password, true))
            .ReturnsAsync(IdentitySignInResult.Success);
        userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

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

        userManagerMock.Setup(x => x.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, model.Password, true))
            .ReturnsAsync(IdentitySignInResult.Success);
        userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin", "User" });

        var result = await controller.Login(model);

        var ok = Assert.IsType<OkObjectResult>(result);
        var token = ok.Value!.GetType().GetProperty("token")?.GetValue(ok.Value) as string;
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

    // --- AdminOnly ---

    [Fact]
    public void AdminOnly_ReturnsOk()
    {
        var (_, _, controller) = CreateController();

        var result = controller.AdminOnly();

        Assert.IsType<OkObjectResult>(result);
    }
}
