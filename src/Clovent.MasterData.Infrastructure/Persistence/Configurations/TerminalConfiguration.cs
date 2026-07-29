using Clovent.MasterData.Terminals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.MasterData.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="Terminal"/>.</summary>
internal sealed class TerminalConfiguration : IEntityTypeConfiguration<Terminal>
{
    public void Configure(EntityTypeBuilder<Terminal> builder)
    {
        builder.ToTable("Terminals", "MasterData");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(ValueConverters.TerminalIdConverter)
            .ValueGeneratedNever();

        builder.Property(t => t.BranchId)
            .HasConversion(ValueConverters.BranchIdConverter)
            .IsRequired();
        builder.HasIndex(t => t.BranchId);

        builder.Property(t => t.Name)
            .HasConversion(ValueConverters.TerminalNameConverter)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Code)
            .HasConversion(ValueConverters.EntityCodeConverter)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.Ignore(t => t.DomainEvents);
    }
}
