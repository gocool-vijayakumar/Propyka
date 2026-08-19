using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Propyka.Api.Modules.Identity.Domain;

namespace Propyka.Api.Modules.Identity;

/// <summary>
/// PasswordSignInAsync runs PreSignInCheck -> CanSignInAsync before it ever
/// checks the password, so overriding this one method blocks deleted accounts
/// at every entry point, including MapIdentityApi's /login and /refresh.
/// </summary>
public class PropykaSignInManager : SignInManager<ApplicationUser>
{
    public PropykaSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<bool> CanSignInAsync(ApplicationUser user)
    {
        if (user.IsDeleted)
        {
            return false;
        }

        return await base.CanSignInAsync(user);
    }
}