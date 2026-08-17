using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Propyka.Api.Data;

namespace Propyka.Api.Authorization;

public class ActiveUserRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Blocking sign-in is not enough on its own: a token issued before the account
/// was deleted stays valid until it expires. This re-checks the user on every
/// authorized request, so a deletion takes effect immediately.
/// </summary>
public class ActiveUserHandler : AuthorizationHandler<ActiveUserRequirement>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ActiveUserHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var user = await _userManager.GetUserAsync(context.User);

        if (user is not null && !user.IsDeleted)
        {
            context.Succeed(requirement);
        }
    }
}