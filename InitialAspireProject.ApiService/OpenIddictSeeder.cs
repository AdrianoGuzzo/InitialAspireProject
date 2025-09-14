using InitialAspireProject.ApiService.Models;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace InitialAspireProject.ApiService.Services;

public class OpenIddictSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync("web-client") == null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "web-client",
                //ClientSecret = "web-secret",
                ConsentType = ConsentTypes.Explicit,
                DisplayName = "Web Client",
                ClientType = ClientTypes.Public, // Use ClientType instead of Type
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Logout,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    Permissions.Prefixes.Scope + "api"
                },
                RedirectUris =
                {
                    new Uri("https://localhost:7053/signin-oidc"),
                    new Uri("http://localhost:7053/signin-oidc")
                },
                PostLogoutRedirectUris =
                {
                    new Uri("https://localhost:7053/signout-callback-oidc"),
                    new Uri("http://localhost:7053/signout-callback-oidc")
                }
            });
        }

        if (await manager.FindByClientIdAsync("api-client") == null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "api-client",
                ClientSecret = "my-secret",
                DisplayName = "My Client App",
                ClientType = OpenIddictConstants.ClientTypes.Confidential,
                Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken
            }
            });
        }
        await CreateAdmin(serviceProvider);
    }
    public static async Task CreateAdmin(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        // 1. Crie o papel de administrador, se n�o existir
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        // 2. Crie o usu�rio admin, se n�o existir
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = "admin@localhost";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            // Defina uma senha forte para o admin
            await userManager.CreateAsync(adminUser, "Admin123$");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
