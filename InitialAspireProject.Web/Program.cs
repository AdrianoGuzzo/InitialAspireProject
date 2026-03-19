using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Shared.Models;
using InitialAspireProject.Web;
using InitialAspireProject.Web.Components;
using InitialAspireProject.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo("/app/dataprotection-keys"))
    .SetApplicationName("InitialAspireProject.Web");

builder.AddServiceDefaults();
builder.AddLocalizationDefaults(["pt-BR", "en", "es"]);
builder.Services.Configure<RequestLocalizationOptions>(options =>
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider()));
builder.AddRedisOutputCache("cacheredis");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddAuthorizationCore(options => options.AddPermissionPolicies());
builder.Services.AddHttpClient();

builder.Services.AddHttpContextAccessor();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services
.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/forbidden";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
    })
    .AddCookie("ExternalCookie")
    .AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
        options.SignInScheme = "ExternalCookie";
        options.CallbackPath = "/signin-google";
        options.Scope.Add("email");
        options.Scope.Add("profile");
    });


builder.Services.AddRazorComponents()
.AddInteractiveServerComponents();
string apiIdentityUrl = "https+http://apiidentity";
builder.Services.AddHttpClient<ILoginService, LoginService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<IRegisterService, RegisterService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<IForgotPasswordService, ForgotPasswordService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<IResetPasswordService, ResetPasswordService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<IConfirmEmailService, ConfirmEmailService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<IProfileService, ProfileService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<IPermissionService, PermissionService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<WeatherApiService>(client => client.BaseAddress = new("https+http://apicore"));
builder.Services.AddHttpClient<IGoogleLoginService, GoogleLoginService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<ITokenRefreshService, TokenRefreshService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient("apiidentity-revoke", client => client.BaseAddress = new Uri(apiIdentityUrl));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseLocalizationDefaults();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.MapGet("/logout", async (HttpContext ctx) =>
{
    var refreshToken = ctx.Session.GetString(SessionConstants.RefreshTokenKey);
    var jwt = ctx.Session.GetString(SessionConstants.TokenKey);
    ctx.Session.Clear();
    await ctx.SignOutAsync("Cookies");

    if (!string.IsNullOrEmpty(refreshToken))
    {
        try
        {
            var httpClientFactory = ctx.RequestServices.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("apiidentity-revoke");
            if (!string.IsNullOrEmpty(jwt))
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
            await httpClient.PostAsJsonAsync("/auth/revoke", new RevokeTokenRequest { RefreshToken = refreshToken });
        }
        catch { /* Best-effort revocation */ }
    }

    return Results.LocalRedirect("/login");
}).AllowAnonymous();

app.MapGet("/set-culture", (string culture, string redirectUri, HttpContext ctx) =>
{
    ctx.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
    return Results.LocalRedirect(redirectUri);
});

app.MapGet("/google-login", (HttpContext ctx, string? returnUrl) =>
{
    var redirectUri = string.IsNullOrEmpty(returnUrl)
        ? "/google-callback"
        : $"/google-callback?returnUrl={Uri.EscapeDataString(returnUrl)}";
    return Results.Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, ["Google"]);
}).AllowAnonymous();

app.MapGet("/google-callback", async (HttpContext ctx, IGoogleLoginService googleLoginService, string? returnUrl) =>
{
    var result = await ctx.AuthenticateAsync("ExternalCookie");
    if (!result.Succeeded)
        return Results.LocalRedirect("/login?error=google_failed");

    var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
    var name = result.Principal?.FindFirst(ClaimTypes.Name)?.Value;

    if (string.IsNullOrEmpty(email))
        return Results.LocalRedirect("/login?error=no_email");

    var tokenResponse = await googleLoginService.GetJwtAsync(email, name ?? email);
    if (tokenResponse?.Token == null)
    {
        await ctx.SignOutAsync("ExternalCookie");
        return Results.LocalRedirect("/login?error=token_failed");
    }

    await ctx.Session.LoadAsync();
    ctx.Session.SetString(SessionConstants.TokenKey, tokenResponse.Token);
    if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
        ctx.Session.SetString(SessionConstants.RefreshTokenKey, tokenResponse.RefreshToken);

    await ctx.SignOutAsync("ExternalCookie");
    var claims = new[] { new Claim(ClaimTypes.Email, email), new Claim(ClaimTypes.Name, name ?? email) };
    await ctx.SignInAsync("Cookies", new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));

    var destination = !string.IsNullOrEmpty(returnUrl) ? returnUrl : "/";
    return Results.LocalRedirect(destination);
}).AllowAnonymous();

app.Run();
