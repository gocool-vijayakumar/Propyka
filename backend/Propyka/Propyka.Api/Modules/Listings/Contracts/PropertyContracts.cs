using System.ComponentModel.DataAnnotations;
using Propyka.Api.Modules.Listings.Domain;

namespace Propyka.Api.Modules.Listings.Contracts;

// ─── Responses ────────────────────────────────────────────────────────────────

/// <summary>Compact shape for search results and cards.</summary>
public sealed record PropertySummaryResponse(
    Guid Id,
    string Slug,
    string Title,
    ListingType ListingType,
    PropertyKind Kind,
    ListingStatus Status,
    decimal Price,
    string Currency,
    RentPeriod? RentPeriod,
    int Bedrooms,
    int Bathrooms,
    double AreaSqFt,
    string City,
    string? Locality,
    string? PrimaryImageUrl,
    DateTimeOffset? PublishedAt);

/// <summary>Full shape for the detail page.</summary>
public sealed record PropertyDetailResponse(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    ListingType ListingType,
    PropertyKind Kind,
    ListingStatus Status,
    decimal Price,
    string Currency,
    RentPeriod? RentPeriod,
    int Bedrooms,
    int Bathrooms,
    double AreaSqFt,
    double? PlotAreaSqFt,
    int? YearBuilt,
    FurnishingLevel? Furnishing,
    int ViewCount,
    AddressResponse Address,
    IReadOnlyList<PropertyImageResponse> Images,
    IReadOnlyList<AmenityResponse> Amenities,
    OwnerResponse Owner,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);

public sealed record AddressResponse(
    string Line1, string? Line2, string? Locality,
    string City, string State, string PostalCode, string Country,
    double? Latitude, double? Longitude);

/// <summary>
/// Url is built from StorageKey by IFileStorage at read time. StorageKey is
/// also sent because the client needs a stable identifier to delete an image,
/// and the URL may change if storage moves.
/// </summary>
public sealed record PropertyImageResponse(
    Guid Id, string StorageKey, string Url, string? Caption, int SortOrder, bool IsPrimary);

public sealed record OwnerResponse(string Id, string FullName);

public sealed record AmenityResponse(int Id, string Name, string Slug);

/// <summary>Returned by POST /api/properties so the client can navigate straight to it.</summary>
public sealed record CreatedPropertyResponse(Guid Id, string Slug);

public sealed record EnquiryResponse(
    Guid Id,
    Guid PropertyId,
    string PropertyTitle,
    string PropertySlug,
    string Name,
    string Email,
    string? Phone,
    string Message,
    EnquiryStatus Status,
    DateTimeOffset CreatedAt);

// ─── Requests ─────────────────────────────────────────────────────────────────

public sealed record CreatePropertyRequest(
    [Required, MaxLength(160)] string Title,
    [MaxLength(4000)] string? Description,
    [Required] ListingType ListingType,
    [Required] PropertyKind Kind,
    [Range(0, 1_000_000_000)] decimal Price,
    RentPeriod? RentPeriod,
    [Range(0, 50)] int Bedrooms,
    [Range(0, 50)] int Bathrooms,
    [Range(0, 1_000_000)] double AreaSqFt,
    [Range(0, 10_000_000)] double? PlotAreaSqFt,
    [Range(1800, 2100)] int? YearBuilt,
    FurnishingLevel? Furnishing,
    [Required] AddressRequest Address,
    int[]? AmenityIds);

public sealed record AddressRequest(
    [Required, MaxLength(200)] string Line1,
    [MaxLength(200)] string? Line2,
    [MaxLength(120)] string? Locality,
    [Required, MaxLength(120)] string City,
    [Required, MaxLength(120)] string State,
    [Required, MaxLength(20)] string PostalCode,
    [Required, MaxLength(80)] string Country,
    [Range(-90, 90)] double? Latitude,
    [Range(-180, 180)] double? Longitude);

public sealed record UpdateImageRequest(
    [MaxLength(200)] string? Caption,
    int SortOrder,
    bool IsPrimary);

public sealed record CreateEnquiryRequest(
    [Required, MaxLength(120)] string Name,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Phone, MaxLength(30)] string? Phone,
    [Required, MaxLength(2000)] string Message);

public sealed record UpdateEnquiryStatusRequest(
    [Required] EnquiryStatus Status);

// ─── Query ────────────────────────────────────────────────────────────────────

/// <summary>
/// Search filters, bound from the query string. A record with init properties
/// rather than a positional record, because positional records need every
/// value supplied and these are all optional.
/// </summary>
public sealed record PropertySearchQuery
{
    [MaxLength(120)]
    public string? Search { get; init; }

    [MaxLength(120)]
    public string? City { get; init; }

    public ListingType? ListingType { get; init; }
    public PropertyKind? Kind { get; init; }

    [Range(0, 1_000_000_000)] public decimal? MinPrice { get; init; }
    [Range(0, 1_000_000_000)] public decimal? MaxPrice { get; init; }
    [Range(0, 50)] public int? MinBedrooms { get; init; }

    public int[]? AmenityIds { get; init; }

    public PropertySort Sort { get; init; } = PropertySort.Newest;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// An enum rather than a magic string. "sort=banana" is now rejected by model
/// binding with a 400 instead of silently falling through to a default.
/// </summary>
public enum PropertySort
{
    Newest,
    PriceAsc,
    PriceDesc
}
