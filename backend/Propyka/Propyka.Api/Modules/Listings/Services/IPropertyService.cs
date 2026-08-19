using Propyka.Api.Common;
using Propyka.Api.Modules.Listings.Contracts;

namespace Propyka.Api.Modules.Listings.Services;

public interface IPropertyService
{
    Task<PagedResult<PropertySummaryResponse>> SearchAsync(
        PropertySearchQuery query, CancellationToken ct = default);

    /// <summary>Public detail lookup. Published listings only; also bumps ViewCount.</summary>
    Task<PropertyDetailResponse?> GetBySlugAsync(
        string slug, CancellationToken ct = default);

    /// <summary>Owner's own listing, drafts included, no view count bump.</summary>
    Task<PropertyDetailResponse?> GetOwnedAsync(
        Guid id, string ownerId, CancellationToken ct = default);

    Task<PagedResult<PropertySummaryResponse>> GetMineAsync(
        string ownerId, int page, int pageSize, CancellationToken ct = default);

    Task<CreatedPropertyResponse> CreateAsync(
        string ownerId, CreatePropertyRequest request, CancellationToken ct = default);

    Task<bool> UpdateAsync(
        Guid id, string ownerId, CreatePropertyRequest request, CancellationToken ct = default);

    Task<bool> PublishAsync(Guid id, string ownerId, CancellationToken ct = default);

    Task<bool> ArchiveAsync(Guid id, string ownerId, CancellationToken ct = default);
}
