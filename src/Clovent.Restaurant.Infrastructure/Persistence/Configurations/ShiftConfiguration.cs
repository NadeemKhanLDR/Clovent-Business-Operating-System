using Clovent.Restaurant.Shifts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="Shift"/> aggregate.</summary>
internal sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts", "Restaurant");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasConversion(ValueConverters.ShiftIdConverter)
            .ValueGeneratedNever();

        builder.Property(s => s.ShiftNumber).IsRequired();
        builder.HasIndex(s => s.ShiftNumber).IsUnique();

        builder.Property(s => s.BranchId)
            .HasConversion(ValueConverters.BranchIdConverter)
            .IsRequired();

        builder.Property(s => s.WarehouseId)
            .HasConversion(ValueConverters.WarehouseIdConverter)
            .IsRequired();

        builder.Property(s => s.TerminalId)
            .HasConversion(ValueConverters.TerminalIdConverter)
            .IsRequired();
        builder.HasIndex(s => s.TerminalId);

        builder.Property(s => s.CashierId)
            .HasConversion(ValueConverters.UserIdConverter)
            .IsRequired();
        builder.HasIndex(s => s.CashierId);

        builder.Property(s => s.CashierName).HasMaxLength(150).IsRequired();

        builder.Property(s => s.OpenedAtUtc).IsRequired();
        builder.Property(s => s.ClosedAtUtc);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(s => s.Status);

        builder.Property(s => s.StartingCash).HasPrecision(18, 2).IsRequired();
        builder.Property(s => s.ExpectedCash).HasPrecision(18, 2).IsRequired();
        builder.Property(s => s.CountedCash).HasPrecision(18, 2).IsRequired();
        builder.Property(s => s.CashVariance).HasPrecision(18, 2).IsRequired();

        builder.Property(s => s.VarianceReason).HasMaxLength(500);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.Property(s => s.CreatedAtUtc).IsRequired();
        builder.Property(s => s.UpdatedAtUtc).IsRequired();

        builder.HasMany(s => s.CashMovements)
            .WithOne()
            .HasForeignKey(m => m.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.CashMovements).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(s => s.DomainEvents);
    }
}
