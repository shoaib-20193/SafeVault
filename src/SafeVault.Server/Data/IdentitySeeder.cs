using Microsoft.AspNetCore.Identity;
using SafeVault.Server.Models;

namespace SafeVault.Server.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["User", "Admin"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{role}': " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        var adminEmail = configuration["AdminSeed:Email"];
        var adminPassword = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(
                admin,
                adminPassword);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to create development admin: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(admin, "Admin");

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to assign Admin role: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        var existingClaims = await userManager.GetClaimsAsync(admin);

        if (!existingClaims.Any(c =>
            c.Type == "Department" &&
            c.Value == "IT"))
        {
            var claimResult = await userManager.AddClaimAsync(
                admin,
                new System.Security.Claims.Claim("Department", "IT"));

            if (!claimResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to assign Department claim: " +
                    string.Join(
                        ", ",
                        claimResult.Errors.Select(e => e.Description)));
            }
        }
    }
}