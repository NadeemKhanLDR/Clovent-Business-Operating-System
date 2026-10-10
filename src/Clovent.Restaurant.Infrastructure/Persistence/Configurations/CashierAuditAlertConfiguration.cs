using Clovent.Restaurant.AuditAlerts;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Shifts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for <see cref="CashierAuditAlert"/> in <c>[Restaurant].[CashierAuditAlerts]</c>.</summary>
public sealed class CashierAuditAlertConfiguration : IEntityTypeConfiguration<CashierAuditAlert>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CashierAuditAlert> builder)
    {
        builder.ToTable("CashierAuditAlerts", "Restaurant");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasConversion(id => id.Value, value => new CashierAuditAlertId(value))
            .ValueGeneratedNever();

        builder.Property(a => a.ShiftId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ShiftId(value.Value) : null)
            .IsRequired(false);

        builder.Property(a => a.OrderId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new OrderId(value.Value) : null)
            .IsRequired(false);

        builder.Property(a => a.CashierId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new Clovent.Identity.Users.UserId(value.Value) : null)
            .IsRequired(false);

        builder.Property(a => a.CashierName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.AnomalyType)
            .HasConversion<string>()
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(a => a.Severity)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.RiskScore)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.SuspectDetailsJson)
            .IsRequired(false);

        builder.Property(a => a.DetectedAtUtc)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.ReviewedBy)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(a => a.ReviewedAtUtc)
            .IsRequired(false);

        builder.Property(a => a.ResolutionNotes)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(a => a.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(a => a.DetectedAtUtc)
            .HasDatabaseName("IX_CashierAuditAlerts_DetectedAtUtc");

        builder.HasIndex(a => a.CashierName)
            .HasDatabaseName("IX_CashierAuditAlerts_CashierName");

        builder.HasIndex(a => a.Status)
            .HasDatabaseName("IX_CashierAuditAlerts_Status");

        builder.HasIndex(a => a.AnomalyType)
            .HasDatabaseName("IX_CashierAuditAlerts_AnomalyType");
    }
}
