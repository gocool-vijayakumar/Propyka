using Propyka.Api.Modules.Listings.Services;

namespace Propyka.Api.Modules.Listings;

public static class ListingsModule
{
    public static IServiceCollection AddListingsModule(this IServiceCollection services)
    {
        // Scoped, because every one of these depends on PropykaDbContext, which
        // is registered scoped by AddDbContext. Injecting a scoped service into
        // a singleton throws at startup.
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IPropertyImageService, PropertyImageService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IEnquiryService, EnquiryService>();
        services.AddScoped<IAmenityService, AmenityService>();

        return services;
    }
}
