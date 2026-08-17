using Microsoft.AspNetCore.Identity;

namespace Propyka.Api.Data;

public static class PropykaRoles
{
    public const string Admin = "Admin";
    public const string Moderator = "Moderator";
    public const string Member = "Member";

    public static readonly string[] All = [Admin, Moderator, Member];
}

public static class IdentitySeeder
{
    /// <summary>
    /// Creates the roles, and promotes the email in configuration key
    /// "Propyka:AdminEmail" to Admin. Set that with user secrets, never in
    /// appsettings.json.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var role in PropykaRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = configuration["Propyka:AdminEmail"];

        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is not null && !await userManager.IsInRoleAsync(admin, PropykaRoles.Admin))
        {
            await userManager.AddToRoleAsync(admin, PropykaRoles.Admin);
        }
    }
}