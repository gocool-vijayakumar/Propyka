using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Propyka.Api.Persistence;

namespace Propyka.Api.Common.Geocoding;

public interface IGeocodingService
{
    /// <summary>Cached address lookup. Returns null when the map service is unavailable.</summary>
    Task<IReadOnlyList<GeocodeResult>?> SearchAsync(string query, CancellationToken ct = default);
}

public sealed partial class GeocodingService : IGeocodingService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly PropykaDbContext _db;
    private readonly IGeocoder _geocoder;
    private readonly GeocodingOptions _options;
    private readonly ILogger<GeocodingService> _logger;

    public GeocodingService(
        PropykaDbContext db, IGeocoder geocoder,
        IOptions<GeocodingOptions> options, ILogger<GeocodingService> logger)
    {
        _db = db;
        _geocoder = geocoder;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Lower case, single spaces, no spaces around commas: "Anna Nagar ,Chennai" == "anna nagar,chennai".</summary>
    public static string Normalise(string query)
    {
        var lower = query.Trim().ToLowerInvariant();
        var singleSpaced = Whitespace().Replace(lower, " ");
        return CommaSpacing().Replace(singleSpaced, ",");
    }

    public async Task<IReadOnlyList<GeocodeResult>?> SearchAsync(string query, CancellationToken ct = default)
    {
        var key = Normalise(query);

        if (key.Length is < 3 or > GeocodeCacheEntry.QueryMaxLength)
        {
            throw new DomainException(
                $"Enter between 3 and {GeocodeCacheEntry.QueryMaxLength} characters to search the map.");
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-_options.CacheDays);
        var cached = await _db.GeocodeCache.FirstOrDefaultAsync(e => e.QueryKey == key, ct);

        if (cached is not null && cached.CreatedAt >= cutoff)
        {
            return JsonSerializer.Deserialize<List<GeocodeResult>>(cached.ResultsJson, Json) ?? [];
        }

        IReadOnlyList<GeocodeResult> results;

        try
        {
            results = await _geocoder.SearchAsync(key, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                   && !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Geocoding failed for a query of length {Length}", key.Length);
            return null;
        }

        var json = JsonSerializer.Serialize(results, Json);

        if (cached is null)
        {
            _db.GeocodeCache.Add(new GeocodeCacheEntry { QueryKey = key, ResultsJson = json, CreatedAt = DateTimeOffset.UtcNow });
        }
        else
        {
            cached.ResultsJson = json;
            cached.CreatedAt = DateTimeOffset.UtcNow;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two people searched the same new address at once and the other request
            // saved it first. The results are the same, so there is nothing to do.
        }

        return results;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"\s*,\s*")]
    private static partial Regex CommaSpacing();
}

public static class GeocodingModule
{
    public static IServiceCollection AddGeocoding(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GeocodingOptions>(configuration.GetSection(GeocodingOptions.SectionName));
        services.AddSingleton<NominatimThrottle>();

        services.AddHttpClient<IGeocoder, NominatimClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<GeocodingOptions>>().Value;
            http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<IGeocodingService, GeocodingService>();

        return services;
    }
}
