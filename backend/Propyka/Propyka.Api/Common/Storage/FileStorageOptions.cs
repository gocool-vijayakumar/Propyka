namespace Propyka.Api.Common.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Folder on disk that files are written into, relative to the content
    /// root. Must sit under wwwroot for UseStaticFiles to serve it.
    /// </summary>
    public string RootPath { get; set; } = "wwwroot/uploads";

    /// <summary>
    /// URL prefix the same files are reachable on. Swap this for a CDN host
    /// later without touching a single controller.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "/uploads";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB

    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp"];
}
