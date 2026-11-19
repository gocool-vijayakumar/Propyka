using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Propyka.Api.Common.Geocoding;

public sealed record GeocodeResult(string DisplayName, double Latitude, double Longitude);

public interface IGeocoder
{
    /// <summary>Looks up an address. Returns an empty list for no match; throws when the service fails.</summary>
    Task<IReadOnlyList<GeocodeResult>> SearchAsync(string query, CancellationToken ct);
}

/// <summary>
/// Calls the Nominatim search API. Typed HttpClient; the throttle makes sure the
/// whole app sends at most one request a second, as the usage policy requires.
/// </summary>
public sealed class NominatimClient : IGeocoder
{
    private readonly HttpClient _http;
    private readonly NominatimThrottle _throttle;
    private readonly GeocodingOptions _options;

    public NominatimClient(HttpClient http, NominatimThrottle throttle, IOptions<GeocodingOptions> options)
    {
        _http = http;
        _throttle = throttle;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<GeocodeResult>> SearchAsync(string query, CancellationToken ct)
    {
        var url = $"search?format=jsonv2&addressdetails=0&limit={_options.MaxResults}" +
                  $"&q={Uri.EscapeDataString(query)}";

        if (!string.IsNullOrWhiteSpace(_options.CountryCodes))
        {
            url += $"&countrycodes={Uri.EscapeDataString(_options.CountryCodes)}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(_options.UserAgent);
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));

        using var response = await _throttle.RunAsync(() => _http.SendAsync(request, ct), ct);
        response.EnsureSuccessStatusCode();

        var places = await response.Content.ReadFromJsonAsync<List<NominatimPlace>>(ct) ?? [];

        return places
            .Select(p => (Place: p, Lat: Parse(p.Lat), Lon: Parse(p.Lon)))
            .Where(x => x.Lat is not null && x.Lon is not null)
            .Select(x => new GeocodeResult(x.Place.DisplayName ?? "", x.Lat!.Value, x.Lon!.Value))
            .ToList();
    }

    // Nominatim returns coordinates as strings.
    private static double? Parse(string? value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

    private sealed record NominatimPlace(
        [property: JsonPropertyName("display_name")] string? DisplayName,
        [property: JsonPropertyName("lat")] string? Lat,
        [property: JsonPropertyName("lon")] string? Lon);
}

/// <summary>
/// Singleton gate: one Nominatim request at a time, with a minimum gap between
/// them. Works for one API instance; several instances would need a shared queue.
/// </summary>
public sealed class NominatimThrottle
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeSpan _gap;
    private DateTimeOffset _last = DateTimeOffset.MinValue;

    public NominatimThrottle(IOptions<GeocodingOptions> options)
    {
        _gap = TimeSpan.FromMilliseconds(options.Value.MinMillisecondsBetweenRequests);
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> call, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);

        try
        {
            var wait = _last + _gap - DateTimeOffset.UtcNow;

            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, ct);
            }

            return await call();
        }
        finally
        {
            _last = DateTimeOffset.UtcNow;
            _gate.Release();
        }
    }
}
