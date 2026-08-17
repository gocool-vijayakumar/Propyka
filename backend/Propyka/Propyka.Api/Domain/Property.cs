using Propyka.Api.Data;

namespace Propyka.Api.Domain;

public class Property
{
    public Guid Id { get; set; }

    /// <summary>URL-safe identifier, e.g. "3-bed-villa-anna-nagar-a1b2c3".</summary>
    public string Slug { get; set; } = string.Empty;

    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ListingType ListingType { get; set; }
    public PropertyKind Kind { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Draft;

    public decimal Price { get; set; }
    public string Currency { get; set; } = "INR";

    /// <summary>Only meaningful when ListingType is Rent.</summary>
    public RentPeriod? RentPeriod { get; set; }

    public int Bedrooms { get; set; }
    public int Bathrooms { get; set; }
    public double AreaSqFt { get; set; }
    public double? PlotAreaSqFt { get; set; }
    public int? YearBuilt { get; set; }
    public FurnishingLevel? Furnishing { get; set; }

    public int ViewCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }

    public PropertyAddress? Address { get; set; }
    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public ICollection<PropertyAmenity> Amenities { get; set; } = new List<PropertyAmenity>();
    public ICollection<Enquiry> Enquiries { get; set; } = new List<Enquiry>();
}