using Microsoft.AspNetCore.Identity;

namespace InitialAspireProject.ApiIdentity
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
            if (!await roleManager.RoleExistsAsync("Admin"))            
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await roleManager.RoleExistsAsync("User"))
                await roleManager.CreateAsync(new IdentityRole("User"));

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
                // Defina uma senha forte para o admin
                await userManager.CreateAsync(adminUser, "Admin123$");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

        }
    }
}
