using Propyka.Api.Modules.Identity;

namespace Propyka.Api.Common.Plans;

/// <summary>
/// What each plan allows. The web app mirrors these numbers for display only;
/// the server is the one that enforces them. Change limits here, nowhere else.
/// Prices are not here: they live in the PlanPrices table so admins can edit them.
/// </summary>
public sealed record PlanDefinition(
    string Name,
    string Tagline,
    bool AdsEnabled,
    int MaxActiveListings,
    int MaxPhotosPerListing,
    int MaxSavedListings,
    IReadOnlyList<string> Features);

public static class PlanCatalog
{
    public static readonly PlanDefinition Free = new(
        Name: "Free",
        Tagline: "Browse for free and list the basics.",
        AdsEnabled: true,
        MaxActiveListings: 3,
        MaxPhotosPerListing: 6,
        MaxSavedListings: 20,
        Features:
        [
            "Browse and search every published listing",
            "Up to 3 active listings",
            "Up to 6 photos per listing",
            "Save up to 20 listings",
            "Advertisements shown on the site"
        ]);

    public static readonly PlanDefinition Pro = new(
        Name: "Pro",
        Tagline: "No ads, and room for a serious portfolio.",
        AdsEnabled: false,
        MaxActiveListings: 50,
        MaxPhotosPerListing: 20,
        MaxSavedListings: 500,
        Features:
        [
            "Everything in Free",
            "No advertisements anywhere on the site",
            "Up to 50 active listings",
            "Up to 20 photos per listing",
            "Save up to 500 listings"
        ]);

    public static IReadOnlyList<PlanDefinition> All { get; } = [Free, Pro];

    public static PlanDefinition? ByName(string name) =>
        All.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Pro members and admins get the Pro plan. Everyone else is Free.</summary>
    public static PlanDefinition ForRoles(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            if (role == PropykaRoles.Pro || role == PropykaRoles.Admin)
            {
                return Pro;
            }
        }

        return Free;
    }
}
