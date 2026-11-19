namespace Propyka.Api.Common.Geocoding;

/// <summary>Settings for turning an address into map coordinates. Section "Geocoding".</summary>
public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    /// <summary>Nominatim server. Point this at a paid or self-hosted instance before heavy use.</summary>
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>
    /// Nominatim's policy requires a User-Agent that names the app. Add a contact
    /// URL or email before production, for example "Propyka/1.0 (+https://your.site/contact)".
    /// </summary>
    public string UserAgent { get; set; } = "Propyka/1.0 (property listings app)";

    /// <summary>ISO country codes to search in, comma separated. Empty searches everywhere.</summary>
    public string CountryCodes { get; set; } = "in";

    /// <summary>Nominatim allows at most one request a second for the whole app.</summary>
    public int MinMillisecondsBetweenRequests { get; set; } = 1100;

    public int CacheDays { get; set; } = 30;

    public int MaxResults { get; set; } = 5;
}
