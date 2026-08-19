using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Propyka.Api.Common;
using Propyka.Api.Modules.Identity.Contracts;
using Propyka.Api.Modules.Identity.Domain;
using Propyka.Api.Persistence;

namespace Propyka.Api.Modules.Identity.Controllers;

/// <summary>
/// Admins manage accounts. Moderators can look but not touch — the read
/// endpoint allows both roles, every write endpoint is Admin only.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = PropykaRoles.Admin + "," + PropykaRoles.Moderator)]
public class AdminController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PropykaDbContext _db;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        PropykaDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    [HttpGet("users")]
    [ProducesResponseType<PagedResult<AdminUserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AdminUserResponse>>> GetUsers(
        string? search = null,
        string status = "active",
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        (page, pageSize) = PagedResult<AdminUserResponse>.Normalise(page, pageSize, 100);

        var query = _db.Users.AsNoTracking();

        query = status.ToLowerInvariant() switch
        {
            "deleted" => query.Where(u => u.IsDeleted),
            "all" => query,
            _ => query.Where(u => !u.IsDeleted)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";

            query = query.Where(u =>
                EF.Functions.ILike(u.Email!, term) ||
                EF.Functions.ILike(u.FirstName, term) ||
                EF.Functions.ILike(u.LastName, term));
        }

        var total = await query.CountAsync(ct);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.CreatedAt,
                u.IsDeleted,
                u.DeletedAt,
                u.LockoutEnd
            })
            .ToListAsync(ct);

        // Roles come from a join table, so they are fetched for the page only.
        var ids = users.Select(u => u.Id).ToList();

        var roles = await (
            from userRole in _db.UserRoles
            join role in _db.Roles on userRole.RoleId equals role.Id
            where ids.Contains(userRole.UserId)
            select new { userRole.UserId, role.Name })
            .ToListAsync(ct);

        var rolesByUser = roles
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Name!).ToList());

        var items = users
            .Select(u => new AdminUserResponse(
                u.Id,
                u.Email ?? string.Empty,
                u.FirstName,
                u.LastName,
                u.CreatedAt,
                u.IsDeleted,
                u.DeletedAt,
                u.LockoutEnd > DateTimeOffset.UtcNow,
                rolesByUser.TryGetValue(u.Id, out var userRoles) ? userRoles : []))
            .ToList();

        return PagedResult<AdminUserResponse>.Create(page, pageSize, total, items);
    }

    [HttpPost("users/{id}/trash")]
    [Authorize(Roles = PropykaRoles.Admin)]
    public async Task<IActionResult> Trash(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (user.Id == currentUserId)
        {
            return Problem("You cannot delete your own admin account.", statusCode: 400);
        }

        if (await _userManager.IsInRoleAsync(user, PropykaRoles.Admin)
            && await CountActiveAdminsAsync() <= 1)
        {
            return Problem("This is the last active admin account.", statusCode: 400);
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.DeletedByUserId = currentUserId;

        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);

        return NoContent();
    }

    [HttpPost("users/{id}/restore")]
    [Authorize(Roles = PropykaRoles.Admin)]
    public async Task<IActionResult> Restore(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        user.IsDeleted = false;
        user.DeletedAt = null;
        user.DeletedByUserId = null;

        await _userManager.UpdateAsync(user);

        return NoContent();
    }

    [HttpPut("users/{id}/roles")]
    [Authorize(Roles = PropykaRoles.Admin)]
    public async Task<IActionResult> SetRoles(string id, SetRolesRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound();
        }

        var invalid = request.Roles.Except(PropykaRoles.All).ToList();

        if (invalid.Count > 0)
        {
            return Problem($"Unknown role: {string.Join(", ", invalid)}.", statusCode: 400);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (user.Id == currentUserId && !request.Roles.Contains(PropykaRoles.Admin))
        {
            return Problem("You cannot remove your own admin role.", statusCode: 400);
        }

        var current = await _userManager.GetRolesAsync(user);

        await _userManager.RemoveFromRolesAsync(user, current.Except(request.Roles));
        await _userManager.AddToRolesAsync(user, request.Roles.Except(current));

        return NoContent();
    }

    private Task<int> CountActiveAdminsAsync()
    {
        return (
            from userRole in _db.UserRoles
            join role in _db.Roles on userRole.RoleId equals role.Id
            join user in _db.Users on userRole.UserId equals user.Id
            where role.Name == PropykaRoles.Admin && !user.IsDeleted
            select user.Id)
            .Distinct()
            .CountAsync();
    }
}
