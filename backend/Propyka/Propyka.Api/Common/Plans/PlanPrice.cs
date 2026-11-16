using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Propyka.Api.Common.Plans;

/// <summary>
/// The price of one plan. Prices live in the database so an admin can run a
/// seasonal offer without a deploy. Limits stay in <see cref="PlanCatalog"/>,
/// because changing a limit changes what the code enforces.
/// </summary>
public sealed class PlanPrice
{
    public string PlanName { get; set; } = string.Empty;
    public int MonthlyPriceInr { get; set; }
    public int YearlyPriceInr { get; set; }

    /// <summary>Optional banner on the pricing page, such as "Diwali offer".</summary>
    public string? OfferLabel { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
    public string? UpdatedByUserId { get; set; }
}

public sealed class PlanPriceConfiguration : IEntityTypeConfiguration<PlanPrice>
{
    public const int OfferLabelMaxLength = 60;

    public void Configure(EntityTypeBuilder<PlanPrice> builder)
    {
        builder.ToTable("PlanPrices");
        builder.HasKey(p => p.PlanName);
        builder.Property(p => p.PlanName).HasMaxLength(40);
        builder.Property(p => p.OfferLabel).HasMaxLength(OfferLabelMaxLength);

        // Fixed timestamp: HasData must produce the same value on every build,
        // or EF generates a new migration each time.
        var seeded = new DateTimeOffset(2026, 11, 13, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            new PlanPrice { PlanName = PlanCatalog.Free.Name, MonthlyPriceInr = 0, YearlyPriceInr = 0, UpdatedAt = seeded },
            new PlanPrice { PlanName = PlanCatalog.Pro.Name, MonthlyPriceInr = 49, YearlyPriceInr = 499, UpdatedAt = seeded });
    }
}
