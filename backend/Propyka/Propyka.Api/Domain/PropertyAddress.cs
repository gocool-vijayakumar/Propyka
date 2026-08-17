namespace Propyka.Api.Domain;

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
}