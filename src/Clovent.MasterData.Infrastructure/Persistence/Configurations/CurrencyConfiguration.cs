using Clovent.MasterData.Currencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.MasterData.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Currency"/>.</summary>
internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies", "MasterData");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(ValueConverters.CurrencyIdConverter)
            .ValueGeneratedNever();

        builder.Property(c => c.Code)
            .HasConversion(ValueConverters.CurrencyCodeConverter)
            .HasMaxLength(3)
            .IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Symbol).HasMaxLength(10).IsRequired();
        builder.Property(c => c.DecimalPlaces).IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
