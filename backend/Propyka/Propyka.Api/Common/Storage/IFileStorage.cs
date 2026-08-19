namespace Propyka.Api.Common.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string folder, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
    string GetPublicUrl(string storageKey);
}