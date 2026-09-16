using Microsoft.AspNetCore.Identity;

namespace LibrarySystem.Data
{
    /// <summary>
    /// Seeds the roles and default accounts required to demonstrate each of the
    /// application's four user experiences (Admin, Reception, Manager, Public).
    /// </summary>
    public static class IdentitySeed
    {
        private const string AdminEmail = "admin@library.com";
        private const string AdminPassword = "Admin@123";

        private const string ReceptionEmail = "reception@library.com";
        private const string ReceptionPassword = "Reception@123";

        private const string ManagerEmail = "manager@library.com";
        private const string ManagerPassword = "Manager@123";

        /// <summary>
        /// Ensures the Admin, Reception and Manager roles exist, then ensures the
        /// three seeded accounts exist and are assigned to their matching role.
        /// Safe to run on every startup - existing roles and users are left untouched.
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

            foreach (var role in new[] { "Admin", "Reception", "Manager" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Admin");
            await EnsureUserAsync(userManager, ReceptionEmail, ReceptionPassword, "Reception");
            await EnsureUserAsync(userManager, ManagerEmail, ManagerPassword, "Manager");
        }

        private static async Task EnsureUserAsync(UserManager<IdentityUser> userManager, string email, string password, string role)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to seed user '{email}': {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
