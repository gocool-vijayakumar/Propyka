using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Propyka.Api.Modules.Identity.Autorization;
using Propyka.Api.Modules.Identity.Domain;
using Propyka.Api.Persistence;

namespace Propyka.Api.Modules.Identity;

/// <summary>
/// Everything the Identity module needs, behind one call. Program.cs should not
/// know that this module uses ASP.NET Identity, roles, or a custom sign-in
/// manager — only that it exists.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services
            .AddIdentityApiEndpoints<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole>()
            .AddSignInManager<PropykaSignInManager>()
            .AddEntityFrameworkStores<PropykaDbContext>();

        services.AddScoped<IAuthorizationHandler, ActiveUserHandler>();

        services.AddAuthorization(options =>
        {
            var activeUser = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ActiveUserRequirement())
                .Build();

            options.AddPolicy(PropykaPolicies.ActiveUser, activeUser);

            // Every plain [Authorize] now also requires a non-deleted account.
            options.DefaultPolicy = activeUser;
        });

        return services;
    }
}

public static class PropykaPolicies
{
    public const string ActiveUser = "ActiveUser";
}