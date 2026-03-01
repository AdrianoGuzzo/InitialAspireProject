using InitialAspireProject.ApiIdentity.Repository.Constants;
using Microsoft.AspNetCore.Identity;

namespace InitialAspireProject.ApiIdentity.Repository
{
    public class Seeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            await CreateUser(serviceProvider);
        }
        private static async Task CreateUser(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync(RoleConstants.Admin))
                await roleManager.CreateAsync(new IdentityRole(RoleConstants.Admin));
            if (!await roleManager.RoleExistsAsync(RoleConstants.User))
                await roleManager.CreateAsync(new IdentityRole(RoleConstants.User));

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
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
    }
}
