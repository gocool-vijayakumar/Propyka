using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Propyka.Api.Common;
using Propyka.Api.Common.Geocoding;

namespace Propyka.Api.Modules.Listings.Controllers;

/// <summary>
/// Address to map coordinates, for placing a listing's pin. Goes through the API
/// rather than the browser so results are cached and the whole app stays inside
/// Nominatim's one-request-a-second limit. Only called when the owner presses
/// "Find on map": Nominatim does not allow search-as-you-type.
/// </summary>
[ApiController]
[Route("api/geocode")]
[Authorize]
public class GeocodeController : ControllerBase
{
    private readonly IGeocodingService _geocoding;

    public GeocodeController(IGeocodingService geocoding)
    {
        _geocoding = geocoding;
    }

    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.Geocode)]
    [ProducesResponseType<IReadOnlyList<GeocodeResult>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<GeocodeResult>>> Search([FromQuery] string? q, CancellationToken ct)
    {
        var results = await _geocoding.SearchAsync(q ?? string.Empty, ct);

        if (results is null)
        {
            return Problem(
                "The map search is not available right now. Place the pin on the map by hand.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(results);
    }
}
