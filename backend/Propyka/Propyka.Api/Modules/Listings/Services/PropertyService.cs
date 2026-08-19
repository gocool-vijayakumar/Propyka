using Microsoft.EntityFrameworkCore;
using Propyka.Api.Common;
using Propyka.Api.Common.Storage;
using Propyka.Api.Modules.Listings.Contracts;
using Propyka.Api.Modules.Listings.Domain;
using Propyka.Api.Persistence;

namespace Propyka.Api.Modules.Listings.Services;

public sealed class PropertyService : IPropertyService
{
    private readonly PropykaDbContext _db;
    private readonly IFileStorage _storage;

    public PropertyService(PropykaDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    // ─── Read ─────────────────────────────────────────────────────────────────

    public async Task<PagedResult<PropertySummaryResponse>> SearchAsync(
        PropertySearchQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = PagedResult<PropertySummaryResponse>.Normalise(
            query.Page, query.PageSize);

        // Only published listings are ever visible publicly.
        var q = _db.Properties
            .AsNoTracking()
            .Where(p => p.Status == ListingStatus.Published);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";

            q = q.Where(p => EF.Functions.ILike(p.Title, term)
                          || EF.Functions.ILike(p.Description, term));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            // ILike rather than ToLower() == ToLower(): wrapping the column in a
            // function stops Postgres using the City index configured in
            // PropertyAddressConfiguration.
            q = q.Where(p => EF.Functions.ILike(p.Address!.City, query.City.Trim()));
        }

        if (query.ListingType is not null) q = q.Where(p => p.ListingType == query.ListingType);
        if (query.Kind is not null) q = q.Where(p => p.Kind == query.Kind);
        if (query.MinPrice is not null) q = q.Where(p => p.Price >= query.MinPrice);
        if (query.MaxPrice is not null) q = q.Where(p => p.Price <= query.MaxPrice);
        if (query.MinBedrooms is not null) q = q.Where(p => p.Bedrooms >= query.MinBedrooms);

        if (query.AmenityIds is { Length: > 0 })
        {
            // A separate EXISTS per amenity, so the listing must have ALL of
            // them. A single Contains() would match ANY, which is not what a
            // user ticking three boxes expects.
            foreach (var amenityId in query.AmenityIds.Distinct())
            {
                q = q.Where(p => p.Amenities.Any(a => a.AmenityId == amenityId));
            }
        }

        q = query.Sort switch
        {
            PropertySort.PriceAsc => q.OrderBy(p => p.Price).ThenBy(p => p.Id),
            PropertySort.PriceDesc => q.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            _ => q.OrderByDescending(p => p.PublishedAt).ThenBy(p => p.Id)
        };

        var total = await q.CountAsync(ct);

        // Projected to an anonymous type first, then mapped in memory. The
        // anonymous shape is what decides the SELECT column list, so this is
        // still one narrow query — but it keeps the storage-key-to-URL call,
        // which EF cannot translate, out of the expression tree.
        var rows = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Slug,
                p.Title,
                p.ListingType,
                p.Kind,
                p.Status,
                p.Price,
                p.Currency,
                p.RentPeriod,
                p.Bedrooms,
                p.Bathrooms,
                p.AreaSqFt,
                City = p.Address!.City,
                p.Address.Locality,
                PrimaryImageKey = p.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.StorageKey)
                    .FirstOrDefault(),
                p.PublishedAt
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

    public async Task<PropertyDetailResponse?> GetBySlugAsync(
        string slug, CancellationToken ct = default)
    {
        var property = await DetailQuery()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Status == ListingStatus.Published, ct);

        if (property is null)
        {
            return null;
        }

        // ExecuteUpdate issues a single UPDATE ... SET "ViewCount" = "ViewCount" + 1.
        // Doing it by loading, incrementing and saving would lose counts whenever
        // two people opened the same listing at the same time.
        await _db.Properties
            .Where(p => p.Id == property.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), ct);

        return MapDetail(property);
    }

    public async Task<PropertyDetailResponse?> GetOwnedAsync(
        Guid id, string ownerId, CancellationToken ct = default)
    {
        var property = await DetailQuery()
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, ct);

        return property is null ? null : MapDetail(property);
    }

    public async Task<PagedResult<PropertySummaryResponse>> GetMineAsync(
        string ownerId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = PagedResult<PropertySummaryResponse>.Normalise(page, pageSize);

        var q = _db.Properties
            .AsNoTracking()
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.UpdatedAt);

        var total = await q.CountAsync(ct);

        var rows = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Slug,
                p.Title,
                p.ListingType,
                p.Kind,
                p.Status,
                p.Price,
                p.Currency,
                p.RentPeriod,
                p.Bedrooms,
                p.Bathrooms,
                p.AreaSqFt,
                City = p.Address!.City,
                p.Address.Locality,
                PrimaryImageKey = p.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.StorageKey)
                    .FirstOrDefault(),
                p.PublishedAt
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

    // ─── Write ────────────────────────────────────────────────────────────────

    public async Task<CreatedPropertyResponse> CreateAsync(
        string ownerId, CreatePropertyRequest request, CancellationToken ct = default)
    {
        await GuardAmenitiesExistAsync(request.AmenityIds, ct);

        var property = new Property
        {
            Id = Guid.NewGuid(),
            Slug = SlugGenerator.Create(request.Title),
            OwnerId = ownerId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            ListingType = request.ListingType,
            Kind = request.Kind,

            // Never taken from the request. A client that could choose its own
            // status could publish a listing that skipped every validation rule.
            Status = ListingStatus.Draft,

            Price = request.Price,
            Currency = "INR",
            RentPeriod = request.ListingType == ListingType.Rent ? request.RentPeriod : null,
            Bedrooms = request.Bedrooms,
            Bathrooms = request.Bathrooms,
            AreaSqFt = request.AreaSqFt,
            PlotAreaSqFt = request.PlotAreaSqFt,
            YearBuilt = request.YearBuilt,
            Furnishing = request.Furnishing,
            Address = BuildAddress(request.Address)
        };

        foreach (var amenityId in (request.AmenityIds ?? []).Distinct())
        {
            property.Amenities.Add(new PropertyAmenity
            {
                PropertyId = property.Id,
                AmenityId = amenityId
            });
        }

        _db.Properties.Add(property);
        await _db.SaveChangesAsync(ct);

        return new CreatedPropertyResponse(property.Id, property.Slug);
    }

    public async Task<bool> UpdateAsync(
        Guid id, string ownerId, CreatePropertyRequest request, CancellationToken ct = default)
    {
        // The OwnerId filter is the authorisation check. Putting it in the WHERE
        // clause rather than a separate if means a non-owner gets a 404 — a 403
        // would confirm the listing exists.
        var property = await _db.Properties
            .Include(p => p.Address)
            .Include(p => p.Amenities)
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, ct);

        if (property is null)
        {
            return false;
        }

        await GuardAmenitiesExistAsync(request.AmenityIds, ct);

        property.Title = request.Title.Trim();
        property.Description = request.Description?.Trim() ?? string.Empty;
        property.ListingType = request.ListingType;
        property.Kind = request.Kind;
        property.Price = request.Price;
        property.RentPeriod = request.ListingType == ListingType.Rent ? request.RentPeriod : null;
        property.Bedrooms = request.Bedrooms;
        property.Bathrooms = request.Bathrooms;
        property.AreaSqFt = request.AreaSqFt;
        property.PlotAreaSqFt = request.PlotAreaSqFt;
        property.YearBuilt = request.YearBuilt;
        property.Furnishing = request.Furnishing;

        // Slug is deliberately not regenerated. Existing links and any search
        // engine that indexed the old one must keep working.

        if (property.Address is null)
        {
            property.Address = BuildAddress(request.Address);
        }
        else
        {
            ApplyAddress(property.Address, request.Address);
        }

        var wanted = (request.AmenityIds ?? []).Distinct().ToHashSet();
        var existing = property.Amenities.Select(a => a.AmenityId).ToHashSet();

        foreach (var removed in property.Amenities.Where(a => !wanted.Contains(a.AmenityId)).ToList())
        {
            property.Amenities.Remove(removed);
        }

        foreach (var added in wanted.Except(existing))
        {
            property.Amenities.Add(new PropertyAmenity
            {
                PropertyId = property.Id,
                AmenityId = added
            });
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> PublishAsync(Guid id, string ownerId, CancellationToken ct = default)
    {
        var property = await _db.Properties
            .Include(p => p.Images)
            .Include(p => p.Address)
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, ct);

        if (property is null)
        {
            return false;
        }

        if (property.Status == ListingStatus.Published)
        {
            return true; // idempotent — publishing twice is not an error
        }

        // DomainException becomes a 400 with this exact message, so the rule
        // lives here rather than being duplicated in the controller.
        if (property.Images.Count == 0)
        {
            throw new DomainException("Add at least one image before publishing.");
        }

        if (property.Address is null)
        {
            throw new DomainException("Add an address before publishing.");
        }

        if (property.Price <= 0)
        {
            throw new DomainException("Set a price above zero before publishing.");
        }

        property.Status = ListingStatus.Published;
        property.PublishedAt ??= DateTimeOffset.UtcNow; // don't reset on re-publish

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ArchiveAsync(Guid id, string ownerId, CancellationToken ct = default)
    {
        var property = await _db.Properties
            .FirstOrDefaultAsync(p => p.Id == id && p.OwnerId == ownerId, ct);

        if (property is null)
        {
            return false;
        }

        property.Status = ListingStatus.Archived;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private IQueryable<Property> DetailQuery()
        => _db.Properties
            .AsNoTracking()
            .Include(p => p.Address)
            .Include(p => p.Owner)
            .Include(p => p.Images)
            .Include(p => p.Amenities)
                .ThenInclude(pa => pa.Amenity);

    private PropertyDetailResponse MapDetail(Property p)
        => new(
            p.Id,
            p.Slug,
            p.Title,
            p.Description,
            p.ListingType,
            p.Kind,
            p.Status,
            p.Price,
            p.Currency,
            p.RentPeriod,
            p.Bedrooms,
            p.Bathrooms,
            p.AreaSqFt,
            p.PlotAreaSqFt,
            p.YearBuilt,
            p.Furnishing,
            p.ViewCount,
            p.Address is null
                ? new AddressResponse("", null, null, "", "", "", "", null, null)
                : new AddressResponse(
                    p.Address.Line1, p.Address.Line2, p.Address.Locality,
                    p.Address.City, p.Address.State, p.Address.PostalCode,
                    p.Address.Country, p.Address.Latitude, p.Address.Longitude),
            p.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => new PropertyImageResponse(
                    i.Id, i.StorageKey, _storage.GetPublicUrl(i.StorageKey),
                    i.Caption, i.SortOrder, i.IsPrimary))
                .ToList(),
            p.Amenities
                .Where(a => a.Amenity is not null)
                .Select(a => new AmenityResponse(a.Amenity!.Id, a.Amenity.Name, a.Amenity.Slug))
                .OrderBy(a => a.Name)
                .ToList(),
            new OwnerResponse(p.OwnerId, p.Owner?.FullName ?? "Propyka member"),
            p.CreatedAt,
            p.PublishedAt);

    private static PropertyAddress BuildAddress(AddressRequest request)
    {
        var address = new PropertyAddress { Id = Guid.NewGuid() };
        ApplyAddress(address, request);
        return address;
    }

    private static void ApplyAddress(PropertyAddress address, AddressRequest request)
    {
        address.Line1 = request.Line1.Trim();
        address.Line2 = request.Line2?.Trim();
        address.Locality = request.Locality?.Trim();
        address.City = request.City.Trim();
        address.State = request.State.Trim();
        address.PostalCode = request.PostalCode.Trim();
        address.Country = request.Country.Trim();
        address.Latitude = request.Latitude;
        address.Longitude = request.Longitude;
    }

    /// <summary>
    /// A bad amenity id would otherwise surface as a foreign key violation from
    /// Postgres — a 500 with a message nobody can act on. This turns it into a
    /// 400 that names the offending ids.
    /// </summary>
    private async Task GuardAmenitiesExistAsync(int[]? amenityIds, CancellationToken ct)
    {
        if (amenityIds is not { Length: > 0 })
        {
            return;
        }

        var requested = amenityIds.Distinct().ToList();

        var found = await _db.Amenities
            .Where(a => requested.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync(ct);

        var missing = requested.Except(found).ToList();

        if (missing.Count > 0)
        {
            throw new DomainException($"Unknown amenity id: {string.Join(", ", missing)}.");
        }
    }
}
