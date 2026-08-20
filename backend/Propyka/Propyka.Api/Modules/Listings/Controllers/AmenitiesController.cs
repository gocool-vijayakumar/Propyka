using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Propyka.Api.Modules.Listings.Contracts;
using Propyka.Api.Modules.Listings.Services;

namespace Propyka.Api.Modules.Listings.Controllers;

[ApiController]
[Route("api/amenities")]
[Produces("application/json")]
public class AmenitiesController : ControllerBase
{
    private readonly IAmenityService _amenities;

    public AmenitiesController(IAmenityService amenities)
    {
        _amenities = amenities;
    }

    /// <summary>
    /// Anonymous, because the public search page needs the list to render its
    /// filter checkboxes before anyone signs in.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<AmenityResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AmenityResponse>>> GetAll(CancellationToken ct)
        => Ok(await _amenities.GetAllAsync(ct));
}
