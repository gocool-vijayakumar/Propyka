using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Propyka.Api.Common;
using Propyka.Api.Modules.Identity.Contracts;
using Propyka.Api.Modules.Identity.Domain;

namespace Propyka.Api.Modules.Identity.Controllers;

/// <summary>
/// MapIdentityApi's built-in /register only accepts email and password, so it
/// cannot populate FirstName / LastName. This controller owns account creation,
/// the current-user endpoint, and self-service deletion.
/// </summary>
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(CreateAccountRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            // Keyed by IdentityError.Code (DuplicateUserName, PasswordTooShort, ...)
            // which is what the Angular Register component reads.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        await _userManager.AddToRoleAsync(user, PropykaRoles.Member);

        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> Me()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfileResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.CreatedAt,
            roles.ToList());
    }

    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        return NoContent();
    }

    /// <summary>
    /// Soft delete. The row is kept so admins can review and restore it; the
    /// current password is required so a stolen token cannot delete an account.
    /// </summary>
    [HttpDelete("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOwnAccount(DeleteAccountRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            ModelState.AddModelError("Password", "That password is not correct.");
            return ValidationProblem(ModelState);
        }

        if (await _userManager.IsInRoleAsync(user, PropykaRoles.Admin))
        {
            ModelState.AddModelError("Role", "An admin account cannot be deleted from here.");
            return ValidationProblem(ModelState);
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.DeletedByUserId = null;

        await _userManager.UpdateAsync(user);

        // Invalidates tokens that depend on the stamp.
        await _userManager.UpdateSecurityStampAsync(user);

        return NoContent();
    }
}
