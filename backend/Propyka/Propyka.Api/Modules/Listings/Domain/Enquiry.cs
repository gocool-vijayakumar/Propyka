using Propyka.Api.Modules.Identity.Domain;

namespace Propyka.Api.Modules.Listings.Domain;

public class Enquiry
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    /// <summary>Null when an anonymous visitor enquires.</summary>
    public string? SenderUserId { get; set; }
    public ApplicationUser? Sender { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Message { get; set; } = string.Empty;

    public EnquiryStatus Status { get; set; } = EnquiryStatus.New;

    public DateTimeOffset CreatedAt { get; set; }
}