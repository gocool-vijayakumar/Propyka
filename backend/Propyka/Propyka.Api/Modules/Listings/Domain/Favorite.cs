using Propyka.Api.Modules.Identity.Domain;

namespace Propyka.Api.Modules.Listings.Domain;

public class Favorite
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}