using Propyka.Api.Data;

namespace Propyka.Api.Domain;

public class Favorite
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}