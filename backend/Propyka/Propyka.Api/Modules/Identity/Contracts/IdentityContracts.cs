using System.ComponentModel.DataAnnotations;

namespace Propyka.Api.Modules.Identity.Contracts;

public sealed record UserProfileResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Roles);

public sealed record AdminUserResponse(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    DateTimeOffset CreatedAt,
    bool IsDeleted,
    DateTimeOffset? DeletedAt,
    bool IsLockedOut,
    IReadOnlyList<string> Roles);

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

public sealed record SetRolesRequest(
    [Required] string[] Roles);
