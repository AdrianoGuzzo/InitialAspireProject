using Blazored.LocalStorage;
using InitialAspireProject.Web;
using InitialAspireProject.Web.Components;
using InitialAspireProject.Web.Services;
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
builder.Services.AddAuthorizationCore();
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
    });


builder.Services.AddRazorComponents()
.AddInteractiveServerComponents();
string apiIdentityUrl = "https+http://apiidentity";
builder.Services.AddHttpClient<ILoginService, LoginService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<IRegisterService, RegisterService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<IForgotPasswordService, ForgotPasswordService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<IResetPasswordService, ResetPasswordService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<IConfirmEmailService, ConfirmEmailService>(client => client.BaseAddress = new(apiIdentityUrl));
builder.Services.AddHttpClient<WeatherApiService>(client => client.BaseAddress = new("https+http://apicore"));

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

app.MapGet("/set-culture", (string culture, string redirectUri, HttpContext ctx) =>
{
    ctx.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });
    return Results.LocalRedirect(redirectUri);
});

app.Run();
