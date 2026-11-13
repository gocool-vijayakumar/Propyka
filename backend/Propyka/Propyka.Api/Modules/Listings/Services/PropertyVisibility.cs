using Propyka.Api.Modules.Listings.Domain;

namespace Propyka.Api.Modules.Listings.Services;

public static class PropertyVisibility
{
    /// <summary>
    /// What the public can see: published listings whose owner still has an
    /// active account. Checked at query time rather than by changing the
    /// listing's status, so an admin restoring the account brings them back.
    /// </summary>
    public static IQueryable<Property> PubliclyVisible(this IQueryable<Property> query) =>
        query.Where(p => p.Status == ListingStatus.Published && !p.Owner!.IsDeleted);
}
