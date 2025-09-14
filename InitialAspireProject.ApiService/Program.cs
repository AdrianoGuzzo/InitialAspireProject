using InitialAspireProject.ApiService.Data;
using InitialAspireProject.ApiService.Models;
using InitialAspireProject.ApiService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();

        // Configurar PostgreSQL
        builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");

        // Configurar Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Configurar OpenIddict
        builder.Services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<ApplicationDbContext>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetLogoutEndpointUris("/connect/logout")
                       .SetUserinfoEndpointUris("/connect/userinfo");

                options.AllowAuthorizationCodeFlow()
                       .AllowRefreshTokenFlow()
                       .AllowPasswordFlow();

                options.RegisterScopes("openid", "profile", "email", "offline_access", "api");

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserinfoEndpointPassthrough()
                       .EnableLogoutEndpointPassthrough();

            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Configurar autenticação JWT
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        builder.Services.AddAuthorization();

        // Add services to the container.
        builder.Services.AddProblemDetails();
        builder.Services.AddControllers();

        var app = builder.Build();
        app.Use(async (context, next) =>
         {
             if (context.Request.Path == "/connect/token" && context.Request.Method == "POST")
             {
                 context.Request.EnableBuffering();
                 var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                 context.Request.Body.Position = 0;

                 Console.WriteLine("===== REQUEST RECEBIDO =====");
                 Console.WriteLine(body);
             }
             await next();
         });
        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();

        // Adicionar middleware de autenticação e autorização
        app.UseAuthentication();
        app.UseAuthorization();

        // Aplicar migrações e seed
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            await OpenIddictSeeder.SeedAsync(scope.ServiceProvider);
        }
        // Endpoint protegido
        app.MapGet("/weatherforecast", () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55)
                ))
                .ToArray();
            return forecast;
        })
        .WithName("GetWeatherForecast")
        .RequireAuthorization();

        app.MapControllers();
        app.MapDefaultEndpoints();

        app.Run();
    }
}

record WeatherForecast(DateOnly Date, int TemperatureC)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}