using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SafeVault.Server.Models;

namespace SafeVault.Server.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();

        // Ensure automatic admin seeding only occurs in Development environment
        if (!environment.IsDevelopment())
        {
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        string[] roles = ["User", "Admin"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create role '{role}': " +
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
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

            var createResult = await userManager.CreateAsync(admin, adminPassword);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to create development admin: " +
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var roleAssignResult = await userManager.AddToRoleAsync(admin, "Admin");

            if (!roleAssignResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to assign Admin role: " +
                    string.Join(", ", roleAssignResult.Errors.Select(e => e.Description)));
            }
        }

        var existingClaims = await userManager.GetClaimsAsync(admin);

        if (!existingClaims.Any(c => c.Type == "Department" && c.Value == "IT"))
        {
            var claimResult = await userManager.AddClaimAsync(
                admin,
                new Claim("Department", "IT"));

            if (!claimResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Failed to assign Department claim: " +
                    string.Join(", ", claimResult.Errors.Select(e => e.Description)));
            }
        }
    }
}