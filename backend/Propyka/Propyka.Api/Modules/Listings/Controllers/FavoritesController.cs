using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Propyka.Api.Common;
using Propyka.Api.Modules.Listings.Contracts;
using Propyka.Api.Modules.Listings.Services;

namespace Propyka.Api.Modules.Listings.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
[Produces("application/json")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favorites;

    public FavoritesController(IFavoriteService favorites)
    {
        _favorites = favorites;
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<PropertySummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PropertySummaryResponse>>> GetMine(
        int page = 1, int pageSize = 20, CancellationToken ct = default)
        => await _favorites.GetMineAsync(CurrentUserId, page, pageSize, ct);

    /// <summary>
    /// Just the ids, so a results page can fill in hearts without loading every
    /// favourited listing in full.
    /// </summary>
    [HttpGet("ids")]
    [ProducesResponseType<IReadOnlyList<Guid>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Guid>>> GetMyIds(CancellationToken ct)
        => Ok(await _favorites.GetMyIdsAsync(CurrentUserId, ct));

    /// <summary>
    /// PUT rather than POST because favouriting is idempotent — tapping the
    /// heart twice should leave one favourite, not fail on a duplicate key.
    /// </summary>
    [HttpPut("{propertyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Add(Guid propertyId, CancellationToken ct)
        => await _favorites.AddAsync(CurrentUserId, propertyId, ct)
            ? NoContent()
            : NotFound();

    [HttpDelete("{propertyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Remove(Guid propertyId, CancellationToken ct)
        => await _favorites.RemoveAsync(CurrentUserId, propertyId, ct)
            ? NoContent()
            : NotFound();

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
