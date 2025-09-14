using Blazored.LocalStorage;
using InitialAspireProject.Web;
using InitialAspireProject.Web.Components;
using InitialAspireProject.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
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
        options.LoginPath = "/";
        options.AccessDeniedPath = "/forbidden";
        options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
options.SlidingExpiration = true;
});


    // Add services to the container.
builder.Services.AddRazorComponents()
.AddInteractiveServerComponents();

    builder.Services.AddHttpClient<ILoginService, LoginService>(client =>
    {    
client.BaseAddress = new("https+http://ApiIdentity");
});
    
        builder.Services.AddHttpClient<WeatherApiClient>(client =>
        {     
    client.BaseAddress = new("https+http://apiservice");
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.UseSession();

app.Run();