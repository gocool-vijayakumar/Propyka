using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Propyka.Api.Common;
using Propyka.Api.Modules.Listings.Contracts;
using Propyka.Api.Modules.Listings.Domain;
using Propyka.Api.Modules.Listings.Services;

namespace Propyka.Api.Modules.Listings.Controllers;

/// <summary>
/// Reading enquiries. Creating one lives on PropertiesController, because the
/// listing is the thing being enquired about and the route reads better as
/// POST /api/properties/{id}/enquiries.
/// </summary>
[ApiController]
[Route("api/enquiries")]
[Authorize]
[Produces("application/json")]
public class EnquiriesController : ControllerBase
{
    private readonly IEnquiryService _enquiries;

    public EnquiriesController(IEnquiryService enquiries)
    {
        _enquiries = enquiries;
    }

    /// <summary>Enquiries people have sent about the caller's listings.</summary>
    [HttpGet("received")]
    [ProducesResponseType<PagedResult<EnquiryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EnquiryResponse>>> Received(
        EnquiryStatus? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
        => await _enquiries.GetReceivedAsync(CurrentUserId, status, page, pageSize, ct);

    /// <summary>Enquiries the caller has sent, when signed in at the time.</summary>
    [HttpGet("sent")]
    [ProducesResponseType<PagedResult<EnquiryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EnquiryResponse>>> Sent(
        int page = 1, int pageSize = 20, CancellationToken ct = default)
        => await _enquiries.GetSentAsync(CurrentUserId, page, pageSize, ct);

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(
        Guid id, UpdateEnquiryStatusRequest request, CancellationToken ct)
        => await _enquiries.SetStatusAsync(id, CurrentUserId, request.Status, ct)
            ? NoContent()
            : NotFound();

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
