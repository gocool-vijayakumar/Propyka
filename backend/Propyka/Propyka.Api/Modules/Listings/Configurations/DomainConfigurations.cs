using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propyka.Api.Modules.Listings.Domain;
using Property = Propyka.Api.Modules.Listings.Domain.Property;

namespace Propyka.Api.Modules.Listings.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Slug).HasMaxLength(160).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Title).HasMaxLength(160).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(4000);

        // Stored as text so the database stays readable and reordering the
        // enum can never silently remap existing rows.
        builder.Property(p => p.ListingType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.RentPeriod).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Furnishing).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();

        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Every public listing query filters on these two together.
        builder.HasIndex(p => new { p.Status, p.ListingType });
        builder.HasIndex(p => p.Price);
        builder.HasIndex(p => p.PublishedAt);
    }
}

public class PropertyAddressConfiguration : IEntityTypeConfiguration<PropertyAddress>
{
    public void Configure(EntityTypeBuilder<PropertyAddress> builder)
    {
        builder.ToTable("PropertyAddresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Line1).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Line2).HasMaxLength(200);
        builder.Property(a => a.Locality).HasMaxLength(120);
        builder.Property(a => a.City).HasMaxLength(120).IsRequired();
        builder.Property(a => a.State).HasMaxLength(120).IsRequired();
        builder.Property(a => a.PostalCode).HasMaxLength(20).IsRequired();
        builder.Property(a => a.Country).HasMaxLength(80).IsRequired();

        builder.HasOne(a => a.Property)
            .WithOne(p => p.Address)
            .HasForeignKey<PropertyAddress>(a => a.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.PropertyId).IsUnique();
        builder.HasIndex(a => a.City);
    }
}

public class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.ToTable("PropertyImages");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.StorageKey).HasMaxLength(400).IsRequired();
        builder.Property(i => i.Caption).HasMaxLength(200);
        builder.Property(i => i.UploadedAt).HasDefaultValueSql("now()");

        builder.HasOne(i => i.Property)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.PropertyId, i.SortOrder });
    }
}

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("Amenities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).HasMaxLength(60).IsRequired();
        builder.Property(a => a.Slug).HasMaxLength(60).IsRequired();

        builder.HasIndex(a => a.Slug).IsUnique();

        builder.HasData(
            new Amenity { Id = 1, Name = "Parking", Slug = "parking" },
            new Amenity { Id = 2, Name = "Lift", Slug = "lift" },
            new Amenity { Id = 3, Name = "Power backup", Slug = "power-backup" },
            new Amenity { Id = 4, Name = "Security", Slug = "security" },
            new Amenity { Id = 5, Name = "Swimming pool", Slug = "swimming-pool" },
            new Amenity { Id = 6, Name = "Gym", Slug = "gym" },
            new Amenity { Id = 7, Name = "Garden", Slug = "garden" },
            new Amenity { Id = 8, Name = "Air conditioning", Slug = "air-conditioning" },
            new Amenity { Id = 9, Name = "Water supply", Slug = "water-supply" },
            new Amenity { Id = 10, Name = "Pet friendly", Slug = "pet-friendly" }
        );
    }
}

public class PropertyAmenityConfiguration : IEntityTypeConfiguration<PropertyAmenity>
{
    public void Configure(EntityTypeBuilder<PropertyAmenity> builder)
    {
        builder.ToTable("PropertyAmenities");

        builder.HasKey(pa => new { pa.PropertyId, pa.AmenityId });

        builder.HasOne(pa => pa.Property)
            .WithMany(p => p.Amenities)
            .HasForeignKey(pa => pa.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.Amenity)
            .WithMany(a => a.Properties)
            .HasForeignKey(pa => pa.AmenityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("Favorites");

        // Composite key means a user cannot favourite the same listing twice.
        builder.HasKey(f => new { f.UserId, f.PropertyId });

        builder.Property(f => f.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.Property)
            .WithMany()
            .HasForeignKey(f => f.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.PropertyId);
    }
}

public class EnquiryConfiguration : IEntityTypeConfiguration<Enquiry>
{
    public void Configure(EntityTypeBuilder<Enquiry> builder)
    {
        builder.ToTable("Enquiries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(30);
        builder.Property(e => e.Message).HasMaxLength(2000).IsRequired();

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne(e => e.Property)
            .WithMany(p => p.Enquiries)
            .HasForeignKey(e => e.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Keep the enquiry if the sender deletes their account.
        builder.HasOne(e => e.Sender)
            .WithMany()
            .HasForeignKey(e => e.SenderUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => new { e.PropertyId, e.CreatedAt });
    }
}