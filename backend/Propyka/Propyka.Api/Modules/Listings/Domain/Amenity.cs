namespace Propyka.Api.Modules.Listings.Domain;

public class Amenity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public ICollection<PropertyAmenity> Properties { get; set; } = new List<PropertyAmenity>();
}