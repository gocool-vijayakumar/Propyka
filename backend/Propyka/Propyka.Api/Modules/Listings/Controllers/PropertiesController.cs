using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Propyka.Api.Common;
using Propyka.Api.Modules.Listings.Contracts;
using Propyka.Api.Modules.Listings.Services;

namespace Propyka.Api.Modules.Listings.Controllers;

[ApiController]
[Route("api/properties")]
[Produces("application/json")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _properties;
    private readonly IPropertyImageService _images;
    private readonly IEnquiryService _enquiries;

    public PropertiesController(
        IPropertyService properties,
        IPropertyImageService images,
        IEnquiryService enquiries)
    {
        _properties = properties;
        _images = images;
        _enquiries = enquiries;
    }

    // ─── Public ───────────────────────────────────────────────────────────────

    /// <summary>Public search. Published listings only.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<PropertySummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PropertySummaryResponse>>> Search(
        [FromQuery] PropertySearchQuery query, CancellationToken ct)
        => await _properties.SearchAsync(query, ct);

    /// <summary>
    /// Public detail lookup by slug. Declared after "mine" and "drafts" in this
    /// file for readability only — ASP.NET always prefers a literal segment
    /// over a parameter, so /api/properties/mine can never match this route.
    /// </summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType<PropertyDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyDetailResponse>> GetBySlug(
        string slug, CancellationToken ct)
    {
        var result = await _properties.GetBySlugAsync(slug, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Send an enquiry about a listing. Open to signed-out visitors.</summary>
    [HttpPost("{id:guid}/enquiries")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> CreateEnquiry(
        Guid id, CreateEnquiryRequest request, CancellationToken ct)
    {
        // Null for anonymous visitors; the entity allows that on purpose.
        var senderId = User.Identity?.IsAuthenticated == true ? CurrentUserId : null;

        var enquiryId = await _enquiries.CreateAsync(id, senderId, request, ct);

        // 202, not 201: the caller has no endpoint to read this back from —
        // only the listing owner can see enquiries.
        return Accepted(new { id = enquiryId });
    }

    // ─── Owner ────────────────────────────────────────────────────────────────

    /// <summary>The signed-in user's own listings, drafts included.</summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType<PagedResult<PropertySummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PropertySummaryResponse>>> Mine(
        int page = 1, int pageSize = 20, CancellationToken ct = default)
        => await _properties.GetMineAsync(CurrentUserId, page, pageSize, ct);

    /// <summary>
    /// Full detail of the caller's own listing, whatever its status. The public
    /// slug endpoint deliberately hides drafts, so the edit form needs this.
    /// </summary>
    [HttpGet("mine/{id:guid}")]
    [Authorize]
    [ProducesResponseType<PropertyDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropertyDetailResponse>> GetOwned(Guid id, CancellationToken ct)
    {
        var result = await _properties.GetOwnedAsync(id, CurrentUserId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType<CreatedPropertyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatedPropertyResponse>> Create(
        CreatePropertyRequest request, CancellationToken ct)
    {
        var created = await _properties.CreateAsync(CurrentUserId, request, ct);

        // The route value must be the slug, not the id — GetBySlug takes a slug.
        // Passing the id produced a Location header that 404s.
        return CreatedAtAction(nameof(GetBySlug), new { slug = created.Slug }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, CreatePropertyRequest request, CancellationToken ct)
        => await _properties.UpdateAsync(id, CurrentUserId, request, ct)
            ? NoContent()
            : NotFound();

    [HttpPost("{id:guid}/publish")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
        => await _properties.PublishAsync(id, CurrentUserId, ct) ? NoContent() : NotFound();

    [HttpPost("{id:guid}/archive")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
        => await _properties.ArchiveAsync(id, CurrentUserId, ct) ? NoContent() : NotFound();

    // ─── Images ───────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/images")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType<PropertyImageResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PropertyImageResponse>> UploadImage(
        Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(nameof(file), "Choose an image to upload.");
            return ValidationProblem(ModelState);
        }

        await using var stream = file.OpenReadStream();

        var image = await _images.AddAsync(
            id, CurrentUserId, stream, file.FileName, file.Length, ct);

        return Created(image.Url, image);
    }

    [HttpPut("{id:guid}/images/{imageId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateImage(
        Guid id, Guid imageId, UpdateImageRequest request, CancellationToken ct)
        => await _images.UpdateAsync(id, imageId, CurrentUserId, request, ct)
            ? NoContent()
            : NotFound();

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken ct)
        => await _images.DeleteAsync(id, imageId, CurrentUserId, ct)
            ? NoContent()
            : NotFound();

    /// <summary>
    /// Non-null because every action here either carries [Authorize] or checks
    /// IsAuthenticated before reading it.
    /// </summary>
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
