using Clovent.MasterData.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.MasterData.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="BusinessSettings"/>.</summary>
internal sealed class BusinessSettingsConfiguration : IEntityTypeConfiguration<BusinessSettings>
{
    public void Configure(EntityTypeBuilder<BusinessSettings> builder)
    {
        builder.ToTable("BusinessSettings", "MasterData");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(ValueConverters.BusinessSettingsIdConverter)
            .ValueGeneratedNever();

        builder.Property(s => s.OrganizationId)
            .HasConversion(ValueConverters.OrganizationIdConverter)
            .IsRequired();
        builder.HasIndex(s => s.OrganizationId).IsUnique();

        builder.Property(s => s.DefaultCurrencyId)
            .HasConversion(ValueConverters.CurrencyIdConverter)
            .IsRequired();

        builder.Property(s => s.DefaultLanguageId)
            .HasConversion(ValueConverters.LanguageIdConverter)
            .IsRequired();

        builder.Property(s => s.DefaultTimeZoneId)
            .HasConversion(ValueConverters.TimeZoneEntryIdConverter)
            .IsRequired();

        builder.Property(s => s.DefaultFiscalYearId)
            .HasConversion(ValueConverters.NullableFiscalYearIdConverter);

        builder.Property(s => s.DateFormat).HasMaxLength(20).IsRequired();

        builder.Property(s => s.CreatedAtUtc).IsRequired();
        builder.Property(s => s.UpdatedAtUtc).IsRequired();

        builder.Ignore(s => s.DomainEvents);
    }
}
