using InitialAspireProject.Bff.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.AddLocalizationDefaults(["pt-BR", "en", "es"]);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "BFF API", Version = "v1" });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme"
            };

            c.AddSecurityDefinition("Bearer", securityScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
        });

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
        });

        builder.Services.AddAuthorization(options => options.AddPermissionPolicies());

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                }
                else
                {
                    var origins = builder.Configuration["Cors:AllowedOrigins"]?
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        ?? [];
                    if (origins.Length == 0)
                        throw new InvalidOperationException("Cors:AllowedOrigins must be configured in production.");
                    policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader();
                }
            });
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("bff-auth-strict", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4,
                        PermitLimit = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
            options.AddPolicy("bff-auth-standard", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4,
                        PermitLimit = 15,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }
                return ValueTask.CompletedTask;
            };
        });

        builder.Services.AddHttpClient<IIdentityProxyService, IdentityProxyService>(
            client => client.BaseAddress = new Uri("https+http://apiidentity"));
        builder.Services.AddHttpClient<ICoreProxyService, CoreProxyService>(
            client => client.BaseAddress = new Uri("https+http://apicore"));

        var app = builder.Build();

        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
        }

        app.UseCors();
        app.UseForwardedHeaders();
        app.UseRateLimiter();

        app.UseLocalizationDefaults();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
