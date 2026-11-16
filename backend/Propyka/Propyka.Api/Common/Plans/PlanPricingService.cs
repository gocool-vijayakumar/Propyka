using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Propyka.Api.Persistence;

namespace Propyka.Api.Common.Plans;

/// <summary>One plan as the pricing page shows it: limits from code, prices from the database.</summary>
public sealed record PlanResponse(
    string Name,
    string Tagline,
    int MonthlyPriceInr,
    int YearlyPriceInr,
    string? OfferLabel,
    bool AdsEnabled,
    int MaxActiveListings,
    int MaxPhotosPerListing,
    int MaxSavedListings,
    IReadOnlyList<string> Features);

/// <summary>A plan's price as the admin console shows it, with who changed it last.</summary>
public sealed record AdminPlanPriceResponse(
    string PlanName,
    int MonthlyPriceInr,
    int YearlyPriceInr,
    string? OfferLabel,
    bool IsEditable,
    DateTimeOffset UpdatedAt,
    string? UpdatedByName);

public sealed record UpdatePlanPriceRequest(
    [Range(0, 1_000_000)] int MonthlyPriceInr,
    [Range(0, 10_000_000)] int YearlyPriceInr,
    [MaxLength(PlanPriceConfiguration.OfferLabelMaxLength)] string? OfferLabel);

public interface IPlanPricingService
{
    Task<IReadOnlyList<PlanResponse>> GetCatalogueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminPlanPriceResponse>> GetPricesAsync(CancellationToken ct = default);
    Task<AdminPlanPriceResponse> UpdatePriceAsync(
        string planName, UpdatePlanPriceRequest request, string adminUserId, CancellationToken ct = default);
}

public sealed class PlanPricingService : IPlanPricingService
{
    private readonly PropykaDbContext _db;

    public PlanPricingService(PropykaDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PlanResponse>> GetCatalogueAsync(CancellationToken ct = default)
    {
        var prices = await _db.PlanPrices.AsNoTracking().ToDictionaryAsync(p => p.PlanName, ct);

        return PlanCatalog.All
            .Select(plan =>
            {
                // A missing row (should not happen after the migration) shows as free
                // rather than failing the whole pricing page.
                prices.TryGetValue(plan.Name, out var price);

                return new PlanResponse(
                    plan.Name, plan.Tagline,
                    price?.MonthlyPriceInr ?? 0, price?.YearlyPriceInr ?? 0, price?.OfferLabel,
                    plan.AdsEnabled, plan.MaxActiveListings, plan.MaxPhotosPerListing,
                    plan.MaxSavedListings, plan.Features);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<AdminPlanPriceResponse>> GetPricesAsync(CancellationToken ct = default)
    {
        var rows = await (
            from price in _db.PlanPrices.AsNoTracking()
            join user in _db.Users on price.UpdatedByUserId equals user.Id into editors
            from editor in editors.DefaultIfEmpty()
            select new { price, EditorName = editor == null ? null : editor.FirstName + " " + editor.LastName })
            .ToListAsync(ct);

        // Keep the catalogue order (Free, then Pro) rather than the table's.
        return PlanCatalog.All
            .Select(plan => rows.FirstOrDefault(r => r.price.PlanName == plan.Name))
            .Where(r => r is not null)
            .Select(r => ToAdminResponse(r!.price, r.EditorName))
            .ToList();
    }

    public async Task<AdminPlanPriceResponse> UpdatePriceAsync(
        string planName, UpdatePlanPriceRequest request, string adminUserId, CancellationToken ct = default)
    {
        var plan = PlanCatalog.ByName(planName)
            ?? throw new NotFoundException($"There is no plan called {planName}.");

        if (!IsEditable(plan.Name))
        {
            throw new DomainException("The Free plan always costs nothing. Only paid plans can be repriced.");
        }

        if (request.MonthlyPriceInr <= 0 || request.YearlyPriceInr <= 0)
        {
            throw new DomainException("Monthly and yearly prices must both be above zero.");
        }

        var price = await _db.PlanPrices.FirstOrDefaultAsync(p => p.PlanName == plan.Name, ct);

        if (price is null)
        {
            price = new PlanPrice { PlanName = plan.Name };
            _db.PlanPrices.Add(price);
        }

        price.MonthlyPriceInr = request.MonthlyPriceInr;
        price.YearlyPriceInr = request.YearlyPriceInr;
        price.OfferLabel = string.IsNullOrWhiteSpace(request.OfferLabel) ? null : request.OfferLabel.Trim();
        price.UpdatedAt = DateTimeOffset.UtcNow;
        price.UpdatedByUserId = adminUserId;

        await _db.SaveChangesAsync(ct);

        var editor = await _db.Users.AsNoTracking()
            .Where(u => u.Id == adminUserId)
            .Select(u => u.FirstName + " " + u.LastName)
            .FirstOrDefaultAsync(ct);

        return ToAdminResponse(price, editor);
    }

    /// <summary>Freemium: the Free plan is never charged for.</summary>
    private static bool IsEditable(string planName) => planName != PlanCatalog.Free.Name;

    private static AdminPlanPriceResponse ToAdminResponse(PlanPrice price, string? editorName) =>
        new(price.PlanName, price.MonthlyPriceInr, price.YearlyPriceInr, price.OfferLabel,
            IsEditable(price.PlanName), price.UpdatedAt, editorName);
}
