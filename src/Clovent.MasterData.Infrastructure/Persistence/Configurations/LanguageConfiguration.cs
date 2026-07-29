using Clovent.MasterData.Languages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.MasterData.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Language"/>.</summary>
internal sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages", "MasterData");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id)
            .HasConversion(ValueConverters.LanguageIdConverter)
            .ValueGeneratedNever();

        builder.Property(l => l.Code)
            .HasConversion(ValueConverters.LanguageCodeConverter)
            .HasMaxLength(2)
            .IsRequired();
        builder.HasIndex(l => l.Code).IsUnique();

        builder.Property(l => l.Name).HasMaxLength(100).IsRequired();
        builder.Property(l => l.NativeName).HasMaxLength(100).IsRequired();

        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.CreatedAtUtc).IsRequired();

        builder.Ignore(l => l.DomainEvents);
    }
}
