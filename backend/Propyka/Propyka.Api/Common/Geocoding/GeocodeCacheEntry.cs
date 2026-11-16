using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propyka.Api.Common.Geocoding;

/// <summary>
/// One cached Nominatim answer. Nominatim asks apps to cache, and repeating the
/// same query gets a client blocked, so every lookup is stored by its normalised text.
/// </summary>
public sealed class GeocodeCacheEntry
{
    public const int QueryMaxLength = 300;

    public string QueryKey { get; set; } = string.Empty;

    /// <summary>The results as JSON. An empty array is cached too, so misses are not retried.</summary>
    public string ResultsJson { get; set; } = "[]";

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class GeocodeCacheEntryConfiguration : IEntityTypeConfiguration<GeocodeCacheEntry>
{
    public void Configure(EntityTypeBuilder<GeocodeCacheEntry> builder)
    {
        builder.ToTable("GeocodeCache");
        builder.HasKey(e => e.QueryKey);
        builder.Property(e => e.QueryKey).HasMaxLength(GeocodeCacheEntry.QueryMaxLength);
        builder.Property(e => e.ResultsJson).HasColumnType("jsonb");
    }
}
