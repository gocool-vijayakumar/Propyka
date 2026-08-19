using Microsoft.EntityFrameworkCore;
using Propyka.Api.Common;
using Propyka.Api.Common.Storage;
using Propyka.Api.Modules.Listings.Contracts;
using Propyka.Api.Modules.Listings.Domain;
using Propyka.Api.Persistence;

namespace Propyka.Api.Modules.Listings.Services;

public interface IFavoriteService
{
    Task<PagedResult<PropertySummaryResponse>> GetMineAsync(
        string userId, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Idempotent add. Returns false if the listing does not exist.</summary>
    Task<bool> AddAsync(string userId, Guid propertyId, CancellationToken ct = default);

    Task<bool> RemoveAsync(string userId, Guid propertyId, CancellationToken ct = default);

    /// <summary>Ids the user has favourited, for highlighting hearts on a results page.</summary>
    Task<IReadOnlyList<Guid>> GetMyIdsAsync(string userId, CancellationToken ct = default);
}

public sealed class FavoriteService : IFavoriteService
{
    private readonly PropykaDbContext _db;
    private readonly IFileStorage _storage;

    public FavoriteService(PropykaDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<PagedResult<PropertySummaryResponse>> GetMineAsync(
        string userId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PagedResult<PropertySummaryResponse>.Normalise(page, pageSize);

        var q = _db.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt);

        var total = await q.CountAsync(ct);

        var rows = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.Property!.Id,
                f.Property.Slug,
                f.Property.Title,
                f.Property.ListingType,
                f.Property.Kind,
                f.Property.Status,
                f.Property.Price,
                f.Property.Currency,
                f.Property.RentPeriod,
                f.Property.Bedrooms,
                f.Property.Bathrooms,
                f.Property.AreaSqFt,
                City = f.Property.Address!.City,
                f.Property.Address.Locality,
                PrimaryImageKey = f.Property.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.StorageKey)
                    .FirstOrDefault(),
                f.Property.PublishedAt
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new PropertySummaryResponse(
                r.Id, r.Slug, r.Title, r.ListingType, r.Kind, r.Status,
                r.Price, r.Currency, r.RentPeriod, r.Bedrooms, r.Bathrooms, r.AreaSqFt,
                r.City, r.Locality,
                r.PrimaryImageKey is null ? null : _storage.GetPublicUrl(r.PrimaryImageKey),
                r.PublishedAt))
            .ToList();

        return PagedResult<PropertySummaryResponse>.Create(page, pageSize, total, items);
    }

    public async Task<bool> AddAsync(string userId, Guid propertyId, CancellationToken ct = default)
    {
        var exists = await _db.Properties.AnyAsync(p => p.Id == propertyId, ct);

        if (!exists)
        {
            return false;
        }

        var already = await _db.Favorites
            .AnyAsync(f => f.UserId == userId && f.PropertyId == propertyId, ct);

        // Idempotent: the endpoint is a PUT, so calling it twice must not be an
        // error. The composite primary key would otherwise throw on the second
        // call and surface as a 500.
        if (already)
        {
            return true;
        }

        _db.Favorites.Add(new Favorite
        {
            UserId = userId,
            PropertyId = propertyId,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveAsync(string userId, Guid propertyId, CancellationToken ct = default)
    {
        var deleted = await _db.Favorites
            .Where(f => f.UserId == userId && f.PropertyId == propertyId)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    public async Task<IReadOnlyList<Guid>> GetMyIdsAsync(
        string userId, CancellationToken ct = default)
        => await _db.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.PropertyId)
            .ToListAsync(ct);
}
