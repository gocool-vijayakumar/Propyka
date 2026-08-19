using Microsoft.EntityFrameworkCore;
using Propyka.Api.Common;
using Propyka.Api.Modules.Listings.Contracts;
using Propyka.Api.Modules.Listings.Domain;
using Propyka.Api.Persistence;

namespace Propyka.Api.Modules.Listings.Services;

public interface IEnquiryService
{
    Task<Guid> CreateAsync(
        Guid propertyId, string? senderUserId, CreateEnquiryRequest request,
        CancellationToken ct = default);

    /// <summary>Enquiries sent to listings the caller owns.</summary>
    Task<PagedResult<EnquiryResponse>> GetReceivedAsync(
        string ownerId, EnquiryStatus? status, int page, int pageSize,
        CancellationToken ct = default);

    /// <summary>Enquiries the caller sent, for signed-in users.</summary>
    Task<PagedResult<EnquiryResponse>> GetSentAsync(
        string senderUserId, int page, int pageSize, CancellationToken ct = default);

    Task<bool> SetStatusAsync(
        Guid enquiryId, string ownerId, EnquiryStatus status, CancellationToken ct = default);
}

public sealed class EnquiryService : IEnquiryService
{
    private readonly PropykaDbContext _db;

    public EnquiryService(PropykaDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> CreateAsync(
        Guid propertyId, string? senderUserId, CreateEnquiryRequest request,
        CancellationToken ct = default)
    {
        var property = await _db.Properties
            .AsNoTracking()
            .Where(p => p.Id == propertyId)
            .Select(p => new { p.Id, p.OwnerId, p.Status })
            .FirstOrDefaultAsync(ct);

        if (property is null || property.Status != ListingStatus.Published)
        {
            // Unpublished listings are not publicly visible, so an enquiry
            // against one is treated exactly like an enquiry against nothing.
            throw new NotFoundException("That listing was not found.");
        }

        if (senderUserId is not null && senderUserId == property.OwnerId)
        {
            throw new DomainException("You cannot enquire about your own listing.");
        }

        var enquiry = new Enquiry
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            SenderUserId = senderUserId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone?.Trim(),
            Message = request.Message.Trim(),
            Status = EnquiryStatus.New,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Enquiries.Add(enquiry);
        await _db.SaveChangesAsync(ct);

        return enquiry.Id;
    }

    public async Task<PagedResult<EnquiryResponse>> GetReceivedAsync(
        string ownerId, EnquiryStatus? status, int page, int pageSize,
        CancellationToken ct = default)
    {
        (page, pageSize) = PagedResult<EnquiryResponse>.Normalise(page, pageSize);

        var q = _db.Enquiries
            .AsNoTracking()
            .Where(e => e.Property!.OwnerId == ownerId);

        if (status is not null)
        {
            q = q.Where(e => e.Status == status);
        }

        return await PageAsync(q.OrderByDescending(e => e.CreatedAt), page, pageSize, ct);
    }

    public async Task<PagedResult<EnquiryResponse>> GetSentAsync(
        string senderUserId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PagedResult<EnquiryResponse>.Normalise(page, pageSize);

        var q = _db.Enquiries
            .AsNoTracking()
            .Where(e => e.SenderUserId == senderUserId)
            .OrderByDescending(e => e.CreatedAt);

        return await PageAsync(q, page, pageSize, ct);
    }

    public async Task<bool> SetStatusAsync(
        Guid enquiryId, string ownerId, EnquiryStatus status, CancellationToken ct = default)
    {
        var enquiry = await _db.Enquiries
            .Include(e => e.Property)
            .FirstOrDefaultAsync(e => e.Id == enquiryId && e.Property!.OwnerId == ownerId, ct);

        if (enquiry is null)
        {
            return false;
        }

        enquiry.Status = status;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task<PagedResult<EnquiryResponse>> PageAsync(
        IQueryable<Enquiry> query, int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EnquiryResponse(
                e.Id,
                e.PropertyId,
                e.Property!.Title,
                e.Property.Slug,
                e.Name,
                e.Email,
                e.Phone,
                e.Message,
                e.Status,
                e.CreatedAt))
            .ToListAsync(ct);

        return PagedResult<EnquiryResponse>.Create(page, pageSize, total, items);
    }
}
