using Microsoft.AspNetCore.Identity;
using Prode.Domain.Entities;

namespace Prode.Infrastructure.Data.Seed
{
    public static class UserSeed
    {
        public static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Crear rol Admin si no existe
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Crear rol User si no existe
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Crear o actualizar SuperAdmin
            var superAdminEmail = "fede.piedrasanta@gmail.com";
            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

            if (superAdmin == null)
            {
                superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true,
                    FullName = "Super Admin"
                };

                // Usar una contraseña que cumpla con los requisitos de complejidad
                var result = await userManager.CreateAsync(superAdmin, "Sup3rAdm1n@2026!");

                if (result.Succeeded)
                {
                    Console.WriteLine("✅ SuperAdmin creado exitosamente.");
                }
                else
                {
                    Console.WriteLine($"❌ Error al crear SuperAdmin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    return;
                }
            }

            // Asegurar que el SuperAdmin tenga el rol Admin (por si ya existía pero no tenía el rol)
            if (!await userManager.IsInRoleAsync(superAdmin, "Admin"))
            {
                await userManager.AddToRoleAsync(superAdmin, "Admin");
                Console.WriteLine("✅ Rol Admin asignado al SuperAdmin.");
            }
            else
            {
                Console.WriteLine("ℹ️ SuperAdmin ya existe y tiene rol Admin.");
            }
        }
    }
}