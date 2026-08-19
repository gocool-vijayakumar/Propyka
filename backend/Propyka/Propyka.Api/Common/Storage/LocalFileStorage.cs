using Microsoft.Extensions.Options;

namespace Propyka.Api.Common.Storage;

/// <summary>
/// Writes uploads to the local disk under wwwroot. Fine for development and a
/// single server; when you move to S3 or Azure Blob you write one new class
/// implementing IFileStorage and change one line in Program.cs. No controller
/// or service changes, because nothing outside this file knows where bytes go.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly FileStorageOptions _options;
    private readonly string _absoluteRoot;

    public LocalFileStorage(
        IOptions<FileStorageOptions> options,
        IWebHostEnvironment environment)
    {
        _options = options.Value;

        _absoluteRoot = Path.Combine(environment.ContentRootPath, _options.RootPath);

        Directory.CreateDirectory(_absoluteRoot);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        string folder,
        CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!_options.AllowedExtensions.Contains(extension))
        {
            throw new DomainException(
                $"Files of type {extension} are not allowed. " +
                $"Allowed: {string.Join(", ", _options.AllowedExtensions)}.");
        }

        // The client's filename is never used on disk. It could contain "../",
        // a null byte, or a name that overwrites another user's file.
        var safeFolder = Sanitise(folder);
        var generatedName = $"{Guid.NewGuid():N}{extension}";

        // Date buckets stop any one directory growing to a million entries,
        // which most filesystems handle badly.
        var relativeDirectory = Path.Combine(
            safeFolder,
            DateTime.UtcNow.ToString("yyyy"),
            DateTime.UtcNow.ToString("MM"));

        Directory.CreateDirectory(Path.Combine(_absoluteRoot, relativeDirectory));

        var relativePath = Path.Combine(relativeDirectory, generatedName);
        var absolutePath = Path.Combine(_absoluteRoot, relativePath);

        await using (var target = File.Create(absolutePath))
        {
            await content.CopyToAsync(target, ct);
        }

        // Always store forward slashes. The key ends up in a URL and in the
        // database, and both must look the same whether the API ran on Windows
        // or Linux when the file was uploaded.
        return relativePath.Replace('\\', '/');
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var absolutePath = ResolveWithinRoot(storageKey);

        if (absolutePath is not null && File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    public string GetPublicUrl(string storageKey)
        => $"{_options.PublicBaseUrl.TrimEnd('/')}/{storageKey.TrimStart('/')}";

    /// <summary>
    /// Resolves a stored key to a full path and refuses anything that escapes
    /// the upload root. Without this check a key of "../../appsettings.json"
    /// would let a delete call reach outside the folder.
    /// </summary>
    private string? ResolveWithinRoot(string storageKey)
    {
        var candidate = Path.GetFullPath(Path.Combine(_absoluteRoot, storageKey));
        var root = Path.GetFullPath(_absoluteRoot);

        return candidate.StartsWith(root, StringComparison.Ordinal) ? candidate : null;
    }

    private static string Sanitise(string folder)
    {
        var cleaned = new string(folder
            .Where(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            .ToArray());

        return string.IsNullOrEmpty(cleaned) ? "misc" : cleaned;
    }
}
