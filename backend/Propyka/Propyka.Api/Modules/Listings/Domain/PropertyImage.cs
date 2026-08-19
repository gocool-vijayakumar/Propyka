namespace Propyka.Api.Modules.Listings.Domain;

public class PropertyImage
{
    public Guid Id { get; set; }

    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    /// <summary>
    /// Path or object key, not a full URL — the host can change without a migration.
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;

    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    public DateTimeOffset UploadedAt { get; set; }
}