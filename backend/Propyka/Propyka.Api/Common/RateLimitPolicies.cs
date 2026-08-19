namespace Propyka.Api.Common;

/// <summary>
/// Policy names are matched by string at runtime, so a typo in an
/// [EnableRateLimiting] attribute throws only when that endpoint is first hit.
/// Constants turn that into a compile error.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Anonymous endpoints that write: register, enquiries.</summary>
    public const string PublicWrite = "PublicWrite";

    /// <summary>Authenticated but expensive: image uploads.</summary>
    public const string Upload = "Upload";
}
