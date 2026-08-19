namespace Propyka.Api.Modules.Listings.Domain;

public enum ListingType
{
    Sale,
    Rent
}

public enum PropertyKind
{
    Apartment,
    House,
    Villa,
    Plot,
    Commercial,
    Warehouse
}

public enum ListingStatus
{
    Draft,
    Published,
    UnderOffer,
    Sold,
    Rented,
    Archived
}

public enum RentPeriod
{
    Monthly,
    Yearly
}

public enum FurnishingLevel
{
    Unfurnished,
    SemiFurnished,
    Furnished
}

public enum EnquiryStatus
{
    New,
    Responded,
    Closed
}