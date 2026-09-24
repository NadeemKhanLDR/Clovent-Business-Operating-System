using Clovent.Restaurant.DayClose;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="BusinessDayClose"/> aggregate.</summary>
internal sealed class BusinessDayCloseConfiguration : IEntityTypeConfiguration<BusinessDayClose>
{
    public void Configure(EntityTypeBuilder<BusinessDayClose> builder)
    {
        builder.ToTable("BusinessDayCloses", "Restaurant");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(ValueConverters.BusinessDayCloseIdConverter)
            .ValueGeneratedNever();

        builder.Property(c => c.BranchId)
            .HasConversion(ValueConverters.BranchIdConverter)
            .IsRequired();

        builder.Property(c => c.BusinessDate)
            .IsRequired();

        // Enforce idempotent single day-close per branch and business date at database level (Phase 51)
        builder.HasIndex(c => new { c.BranchId, c.BusinessDate }).IsUnique();

        builder.Property(c => c.ClosedAtUtc).IsRequired();

        builder.Property(c => c.ClosedByUserId)
            .HasConversion(ValueConverters.UserIdConverter)
            .IsRequired();

        builder.Property(c => c.ClosedByUserName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.TotalSales).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.CashSales).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.CardSales).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.OtherPayments).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.Refunds).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.Discounts).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.Tax).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.CashIn).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.CashOut).HasPrecision(18, 2).IsRequired();

        builder.Property(c => c.ShiftCount).IsRequired();
        builder.Property(c => c.TotalShiftVariance).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.OrderCount).IsRequired();

        builder.Property(c => c.Notes).HasMaxLength(1000);
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
