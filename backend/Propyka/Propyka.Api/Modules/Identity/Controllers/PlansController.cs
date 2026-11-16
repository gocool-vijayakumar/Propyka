using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Propyka.Api.Common.Plans;

namespace Propyka.Api.Modules.Identity.Controllers;

[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly IPlanPricingService _pricing;

    public PlansController(IPlanPricingService pricing)
    {
        _pricing = pricing;
    }

    /// <summary>The plan catalogue for the pricing page, with current prices. Public.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<PlanResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlanResponse>>> Get(CancellationToken ct) =>
        Ok(await _pricing.GetCatalogueAsync(ct));
}

/// <summary>
/// Plan prices for the admin console. Admins and moderators can read them;
/// only admins can change them, for example to run a seasonal offer.
/// </summary>
[ApiController]
[Route("api/admin/plans")]
[Authorize(Roles = PropykaRoles.Admin + "," + PropykaRoles.Moderator)]
public class AdminPlansController : ControllerBase
{
    private readonly IPlanPricingService _pricing;

    public AdminPlansController(IPlanPricingService pricing)
    {
        _pricing = pricing;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminPlanPriceResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminPlanPriceResponse>>> GetPrices(CancellationToken ct) =>
        Ok(await _pricing.GetPricesAsync(ct));

    [HttpPut("{planName}/price")]
    [Authorize(Roles = PropykaRoles.Admin)]
    [ProducesResponseType<AdminPlanPriceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminPlanPriceResponse>> UpdatePrice(
        string planName, UpdatePlanPriceRequest request, CancellationToken ct)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        return Ok(await _pricing.UpdatePriceAsync(planName, request, adminId, ct));
    }
}
