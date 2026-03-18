using Bunit;
using InitialAspireProject.Shared.Models;
using InitialAspireProject.Web;
using InitialAspireProject.Web.Components.Pages;
using InitialAspireProject.Web.Resources;
using InitialAspireProject.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class LoginPageTests : Bunit.TestContext
{
    private readonly Mock<ILoginService> _loginServiceMock;
    private readonly Mock<ISession> _sessionMock;

    public LoginPageTests()
    {
        var localizerMock = new Mock<IStringLocalizer<WebMessages>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
                     .Returns<string>(key => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);

        _loginServiceMock = new Mock<ILoginService>();
        Services.AddSingleton(_loginServiceMock.Object);

        var confirmEmailServiceMock = new Mock<IConfirmEmailService>();
        Services.AddSingleton(confirmEmailServiceMock.Object);

        // HttpClient (injected but unused in code-behind)
        Services.AddSingleton(new HttpClient());

        // JwtAuthStateProvider setup
        _sessionMock = new Mock<ISession>();
        var mockAuthService = new Mock<IAuthenticationService>();
        mockAuthService.Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                       .Returns(Task.CompletedTask);
        var serviceProvider = new ServiceCollection()
            .AddSingleton(mockAuthService.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider,
            Session = _sessionMock.Object
        };
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        var authProvider = new JwtAuthStateProvider(mockAccessor.Object, Mock.Of<ILogger<JwtAuthStateProvider>>());
        Services.AddSingleton<AuthenticationStateProvider>(authProvider);
        Services.AddSingleton(authProvider);

        // ProtectedLocalStorage needs JSInterop + DataProtection
        JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
        Services.AddDataProtection();
        Services.AddScoped<ProtectedLocalStorage>();
    }

    [Fact]
    public void Login_InitialRender_ShowsLoginForm()
    {
        var cut = RenderComponent<Login>();

        Assert.NotNull(cut.Find("input#username"));
        Assert.NotNull(cut.Find("input#password"));
        Assert.NotNull(cut.Find("button[type=submit]"));
    }

    [Fact]
    public void Login_InitialRender_NoErrorMessage()
    {
        var cut = RenderComponent<Login>();

        Assert.DoesNotContain("alert-danger", cut.Markup);
    }

    [Fact]
    public void Login_SubmitEmptyFields_ShowsValidationErrors()
    {
        var cut = RenderComponent<Login>();

        // Submit without filling in fields triggers DataAnnotations validation
        cut.Find("form").Submit();

        // Validation messages should appear (from DataAnnotations [Required])
        Assert.NotEmpty(cut.FindAll(".validation-message"));
    }

    [Fact]
    public void Login_HasUsernameAndPasswordInputs()
    {
        var cut = RenderComponent<Login>();

        var usernameInput = cut.Find("input#username");
        var passwordInput = cut.Find("input#password");

        Assert.Equal("password", passwordInput.GetAttribute("type"));
        Assert.NotNull(usernameInput);
    }

    [Fact]
    public void Login_InvalidCredentials_ShowsErrorMessage()
    {
        _loginServiceMock.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                         .ReturnsAsync(new LoginResult());

        var cut = RenderComponent<Login>();

        cut.Find("input#username").Change("user@test.com");
        cut.Find("input#password").Change("wrongpassword");
        cut.Find("form").Submit();

        Assert.Contains("InvalidCredentialsMsg", cut.Markup);
    }

    [Fact]
    public void Login_ValidCredentials_NavigatesToHome()
    {
        // Create a valid JWT token (header.payload.signature)
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"sub":"1","name":"Test User","exp":9999999999}"""));
        var token = $"eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.{payload}.signature";

        _loginServiceMock.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                         .ReturnsAsync(new LoginResult { Token = new LoginResponse { Token = token } });

        var cut = RenderComponent<Login>();
        var nav = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();

        cut.Find("input#username").Change("admin@localhost");
        cut.Find("input#password").Change("Admin123$");
        cut.Find("form").Submit();

        Assert.Equal("http://localhost/", nav.Uri);
    }

    [Fact]
    public void Login_ValidCredentials_WithLocalReturnUrl_NavigatesToReturnUrl()
    {
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"sub":"1","name":"Test User","exp":9999999999}"""));
        var token = $"eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.{payload}.signature";

        _loginServiceMock.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                         .ReturnsAsync(new LoginResult { Token = new LoginResponse { Token = token } });

        var nav = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        nav.NavigateTo("/login?ReturnUrl=%2Fweather");

        var cut = RenderComponent<Login>();

        cut.Find("input#username").Change("admin@localhost");
        cut.Find("input#password").Change("Admin123$");
        cut.Find("form").Submit();

        Assert.EndsWith("/weather", nav.Uri);
    }

    [Fact]
    public void Login_ValidCredentials_WithExternalReturnUrl_NavigatesToHome()
    {
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"sub":"1","name":"Test User","exp":9999999999}"""));
        var token = $"eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.{payload}.signature";

        _loginServiceMock.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                         .ReturnsAsync(new LoginResult { Token = new LoginResponse { Token = token } });

        var nav = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        nav.NavigateTo("/login?ReturnUrl=https%3A%2F%2Fevil.com");

        var cut = RenderComponent<Login>();

        cut.Find("input#username").Change("admin@localhost");
        cut.Find("input#password").Change("Admin123$");
        cut.Find("form").Submit();

        Assert.Equal("http://localhost/", nav.Uri);
    }

    [Fact]
    public void Login_ValidCredentials_WithProtocolRelativeReturnUrl_NavigatesToHome()
    {
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"sub":"1","name":"Test User","exp":9999999999}"""));
        var token = $"eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.{payload}.signature";

        _loginServiceMock.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), default))
                         .ReturnsAsync(new LoginResult { Token = new LoginResponse { Token = token } });

        var nav = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        nav.NavigateTo("/login?ReturnUrl=%2F%2Fevil.com");

        var cut = RenderComponent<Login>();

        cut.Find("input#username").Change("admin@localhost");
        cut.Find("input#password").Change("Admin123$");
        cut.Find("form").Submit();

        Assert.Equal("http://localhost/", nav.Uri);
    }

    [Fact]
    public void Login_ShowsRegisterLink()
    {
        var cut = RenderComponent<Login>();

        Assert.NotNull(cut.Find("a[href='/register']"));
    }

    [Fact]
    public void Login_ShowsForgotPasswordLink()
    {
        var cut = RenderComponent<Login>();

        Assert.NotNull(cut.Find("a[href='/forgot-password']"));
    }
}
