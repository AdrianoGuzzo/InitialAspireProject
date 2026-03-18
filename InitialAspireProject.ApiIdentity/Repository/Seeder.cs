using System.Security.Claims;
using InitialAspireProject.ApiIdentity.Repository.Constants;
using InitialAspireProject.Shared.Constants;
using Microsoft.AspNetCore.Identity;

namespace InitialAspireProject.ApiIdentity.Repository
{
    public class Seeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await CreateRolesAndUser(scope.ServiceProvider, roleManager);
            await SeedPermissionsAsync(roleManager);
        }

        private static async Task CreateRolesAndUser(IServiceProvider scopeProvider, RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync(RoleConstants.Admin))
                await roleManager.CreateAsync(new IdentityRole(RoleConstants.Admin));
            if (!await roleManager.RoleExistsAsync(RoleConstants.User))
                await roleManager.CreateAsync(new IdentityRole(RoleConstants.User));

            var userManager = scopeProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var adminEmail = "admin@localhost";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    FullName = "Admin User",
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(adminUser, "Admin123$");
                await userManager.AddToRoleAsync(adminUser, RoleConstants.Admin);
            }
        }

        internal static async Task SeedPermissionsAsync(RoleManager<IdentityRole> roleManager)
        {
            await SeedRolePermissionsAsync(roleManager, RoleConstants.Admin, PermissionConstants.All);
            await SeedRolePermissionsAsync(roleManager, RoleConstants.User, [PermissionConstants.CanViewReports]);
        }

        private static async Task SeedRolePermissionsAsync(
            RoleManager<IdentityRole> roleManager, string roleName, string[] permissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) return;

            var existingClaims = await roleManager.GetClaimsAsync(role);
            var existingPermissions = existingClaims
                .Where(c => c.Type == PermissionConstants.ClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            foreach (var permission in permissions)
            {
                if (!existingPermissions.Contains(permission))
                    await roleManager.AddClaimAsync(role, new Claim(PermissionConstants.ClaimType, permission));
            }
        }
    }
}
