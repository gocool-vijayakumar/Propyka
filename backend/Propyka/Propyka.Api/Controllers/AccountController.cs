using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Propyka.Api.Data;

namespace Propyka.Api.Controllers;

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
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            createdAt = user.CreatedAt,
            roles
        });
    }

    [HttpPut("me")]
    [Authorize]
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

// NOT named RegisterRequest: MapIdentityApi already publishes a
// Microsoft.AspNetCore.Identity.Data.RegisterRequest, and Swashbuckle keys
// schemas by short type name — two types with one name is a 500 on swagger.json.
public sealed record CreateAccountRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    [Required, MaxLength(60)] string FirstName,
    [Required, MaxLength(60)] string LastName);

public sealed record UpdateProfileRequest(
    [Required, MaxLength(60)] string FirstName,
    [Required, MaxLength(60)] string LastName);

public sealed record DeleteAccountRequest(
    [Required] string Password);