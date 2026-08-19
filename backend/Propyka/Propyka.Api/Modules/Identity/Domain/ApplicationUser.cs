using Microsoft.AspNetCore.Identity;

namespace Propyka.Api.Modules.Identity.Domain;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Soft delete. The row stays; the account stops working. Admins can see
    /// and restore it, the owner is told their account is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Id of the admin who removed the account, or null if self-deleted.</summary>
    public string? DeletedByUserId { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}