using Microsoft.AspNetCore.Identity;

namespace InitialAspireProject.ApiIdentity
{
    public class Seeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            await CreateAdmin(serviceProvider);
        }
        private static async Task CreateAdmin(IServiceProvider serviceProvider)
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
}
