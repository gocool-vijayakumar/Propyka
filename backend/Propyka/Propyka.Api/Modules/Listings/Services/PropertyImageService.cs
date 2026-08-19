using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Propyka.Api.Common;
using Propyka.Api.Common.Storage;
using Propyka.Api.Modules.Listings.Contracts;
using Propyka.Api.Modules.Listings.Domain;
using Propyka.Api.Persistence;

namespace Propyka.Api.Modules.Listings.Services;

public interface IPropertyImageService
{
    /// <summary>
    /// Takes a stream rather than IFormFile deliberately: IFormFile is an
    /// ASP.NET type, and a service that knows about HTTP cannot be reused from
    /// a background job or a seeder.
    /// </summary>
    Task<PropertyImageResponse> AddAsync(
        Guid propertyId, string ownerId, Stream content, string fileName, long length,
        CancellationToken ct = default);

    Task<bool> UpdateAsync(
        Guid propertyId, Guid imageId, string ownerId, UpdateImageRequest request,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(
        Guid propertyId, Guid imageId, string ownerId, CancellationToken ct = default);
}

public sealed class PropertyImageService : IPropertyImageService
{
    private const int MaxImagesPerProperty = 20;

    private readonly PropykaDbContext _db;
    private readonly IFileStorage _storage;
    private readonly FileStorageOptions _options;

    public PropertyImageService(
        PropykaDbContext db,
        IFileStorage storage,
        IOptions<FileStorageOptions> options)
    {
        _db = db;
        _storage = storage;
        _options = options.Value;
    }

    public async Task<PropertyImageResponse> AddAsync(
        Guid propertyId, string ownerId, Stream content, string fileName, long length,
        CancellationToken ct = default)
    {
        if (length <= 0)
        {
            throw new DomainException("The uploaded file is empty.");
        }

        if (length > _options.MaxFileSizeBytes)
        {
            var limitMb = _options.MaxFileSizeBytes / 1024 / 1024;
            throw new DomainException($"Images must be smaller than {limitMb} MB.");
        }

        var property = await _db.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.OwnerId == ownerId, ct);

        if (property is null)
        {
            throw new NotFoundException("That listing was not found.");
        }

        if (property.Images.Count >= MaxImagesPerProperty)
        {
            throw new DomainException($"A listing can have at most {MaxImagesPerProperty} images.");
        }

        // Extension and path safety are enforced inside LocalFileStorage, so
        // every storage backend gets the same rules.
        var storageKey = await _storage.SaveAsync(content, fileName, "properties", ct);

        var image = new PropertyImage
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            StorageKey = storageKey,
            SortOrder = property.Images.Count == 0 ? 0 : property.Images.Max(i => i.SortOrder) + 1,

            // First image uploaded becomes the cover, so a listing is never
            // published with images but no thumbnail.
            IsPrimary = property.Images.Count == 0,
            UploadedAt = DateTimeOffset.UtcNow
        };

        _db.PropertyImages.Add(image);
        await _db.SaveChangesAsync(ct);

        return new PropertyImageResponse(
            image.Id, image.StorageKey, _storage.GetPublicUrl(image.StorageKey),
            image.Caption, image.SortOrder, image.IsPrimary);
    }

    public async Task<bool> UpdateAsync(
        Guid propertyId, Guid imageId, string ownerId, UpdateImageRequest request,
        CancellationToken ct = default)
    {
        var property = await _db.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.OwnerId == ownerId, ct);

        var image = property?.Images.FirstOrDefault(i => i.Id == imageId);

        if (property is null || image is null)
        {
            return false;
        }

        image.Caption = request.Caption?.Trim();
        image.SortOrder = request.SortOrder;

        if (request.IsPrimary)
        {
            // Exactly one primary. Demote the others in the same save so there
            // is never a moment with two cover images.
            foreach (var other in property.Images)
            {
                other.IsPrimary = other.Id == imageId;
            }
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid propertyId, Guid imageId, string ownerId, CancellationToken ct = default)
    {
        var property = await _db.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.OwnerId == ownerId, ct);

        var image = property?.Images.FirstOrDefault(i => i.Id == imageId);

        if (property is null || image is null)
        {
            return false;
        }

        if (property.Status == ListingStatus.Published && property.Images.Count == 1)
        {
            throw new DomainException(
                "A published listing needs at least one image. Archive it first, or add another image.");
        }

        var wasPrimary = image.IsPrimary;
        var storageKey = image.StorageKey;

        _db.PropertyImages.Remove(image);

        if (wasPrimary)
        {
            var replacement = property.Images
                .Where(i => i.Id != imageId)
                .OrderBy(i => i.SortOrder)
                .FirstOrDefault();

            if (replacement is not null)
            {
                replacement.IsPrimary = true;
            }
        }

        await _db.SaveChangesAsync(ct);

        // Deleted from disk only after the row is gone. If this throws, the
        // result is an orphaned file — annoying but harmless. The other order
        // would leave a row pointing at a file that no longer exists, which
        // breaks every page showing that listing.
        await _storage.DeleteAsync(storageKey, ct);

        return true;
    }
}
