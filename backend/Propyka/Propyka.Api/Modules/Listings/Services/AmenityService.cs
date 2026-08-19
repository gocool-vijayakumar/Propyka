using Microsoft.EntityFrameworkCore;
using Propyka.Api.Modules.Listings.Contracts;
using Propyka.Api.Persistence;

namespace Propyka.Api.Modules.Listings.Services;

public interface IAmenityService
{
    Task<IReadOnlyList<AmenityResponse>> GetAllAsync(CancellationToken ct = default);
}

/// <summary>
/// The amenity list is seeded, tiny, and changes about once a year — but the
/// property form cannot render its checkboxes without it, so it needs an
/// endpoint. Hard-coding the list in Angular would mean two sources of truth
/// and ids that silently drift.
/// </summary>
public sealed class AmenityService : IAmenityService
{
    private readonly PropykaDbContext _db;

    public AmenityService(PropykaDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AmenityResponse>> GetAllAsync(CancellationToken ct = default)
        => await _db.Amenities
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AmenityResponse(a.Id, a.Name, a.Slug))
            .ToListAsync(ct);
}
