namespace Propyka.Api.Modules.Listings.Domain;

public class PropertyAddress
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? Locality { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "India";

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>
    /// The point the public may see: exact when the owner allows it, otherwise rounded
    /// to about 110 m. Stored, not computed per request, so search and the map-area
    /// filter run on it. Filtering on the exact point would leak it: shrinking the box
    /// until a listing drops out would reveal where it really is.
    /// </summary>
    public double? PublicLatitude { get; set; }
    public double? PublicLongitude { get; set; }

    /// <summary>Call whenever the coordinates or the owner's exact-location choice change.</summary>
    public void RefreshPublicLocation(bool showExact)
    {
        PublicLatitude = showExact ? Latitude : Approximate(Latitude);
        PublicLongitude = showExact ? Longitude : Approximate(Longitude);
    }

    /// <summary>Rounds to 3 decimal places, about 110 m.</summary>
    public static double? Approximate(double? value) =>
        value is null ? null : Math.Round(value.Value, 3, MidpointRounding.AwayFromZero);
}