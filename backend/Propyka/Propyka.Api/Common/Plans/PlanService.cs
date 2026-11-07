using Microsoft.AspNetCore.Identity;
using Propyka.Api.Modules.Identity.Domain;

namespace Propyka.Api.Common.Plans;

public interface IPlanService
{
    /// <summary>The plan a user is on right now. Unknown or deleted users get Free.</summary>
    Task<PlanDefinition> ForUserAsync(string userId);
}

/// <summary>
/// Reads roles from the database rather than from the token. A plan change then
/// applies on the next request, not when the old token runs out.
/// </summary>
public sealed class PlanService : IPlanService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public PlanService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<PlanDefinition> ForUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || user.IsDeleted)
        {
            return PlanCatalog.Free;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return PlanCatalog.ForRoles(roles);
    }
}
