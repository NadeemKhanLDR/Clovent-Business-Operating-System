using Clovent.Restaurant.Shifts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="CashMovement"/> entity.</summary>
internal sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("CashMovements", "Restaurant");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasConversion(ValueConverters.CashMovementIdConverter)
            .ValueGeneratedNever();

        builder.Property(m => m.ShiftId)
            .HasConversion(ValueConverters.ShiftIdConverter)
            .IsRequired();
        builder.HasIndex(m => m.ShiftId);

        builder.Property(m => m.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(m => m.Reason).HasMaxLength(250).IsRequired();

        builder.Property(m => m.UserId)
            .HasConversion(ValueConverters.UserIdConverter)
            .IsRequired();

        builder.Property(m => m.TimestampUtc).IsRequired();
        builder.Property(m => m.Notes).HasMaxLength(500);
    }
}
