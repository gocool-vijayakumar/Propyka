namespace Propyka.Api.Modules.Listings.Domain;

public class PropertyAmenity
{
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    public int AmenityId { get; set; }
    public Amenity? Amenity { get; set; }
}