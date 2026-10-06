using Clovent.Restaurant.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.Restaurant.Infrastructure.Persistence.Configurations;

/// <summary>EF Core entity configuration for <see cref="OutboxMessage"/>.</summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", "Restaurant");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, value => new OutboxMessageId(value))
            .ValueGeneratedNever();

        builder.Property(m => m.MessageType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.AggregateType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.AggregateId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.IdempotencyKey)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.AttemptCount)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.Property(m => m.AvailableAtUtc)
            .IsRequired();

        builder.Property(m => m.ProcessingStartedAtUtc)
            .IsRequired(false);

        builder.Property(m => m.CompletedAtUtc)
            .IsRequired(false);

        builder.Property(m => m.LastAttemptAtUtc)
            .IsRequired(false);

        builder.Property(m => m.LastError)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(m => m.NextRetryAtUtc)
            .IsRequired(false);

        builder.Property(m => m.Priority)
            .IsRequired();

        builder.Property(m => m.Version)
            .IsConcurrencyToken()
            .IsRequired();

        // Indexes for high-performance polling and safe concurrency
        builder.HasIndex(m => new { m.Status, m.AvailableAtUtc, m.Priority })
            .HasDatabaseName("IX_OutboxMessages_Status_Available_Priority");

        builder.HasIndex(m => m.CorrelationId)
            .HasDatabaseName("IX_OutboxMessages_CorrelationId");

        builder.HasIndex(m => new { m.MessageType, m.Status })
            .HasDatabaseName("IX_OutboxMessages_Type_Status");

        builder.HasIndex(m => m.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("IX_OutboxMessages_IdempotencyKey");

        builder.Ignore(m => m.DomainEvents);
    }
}
