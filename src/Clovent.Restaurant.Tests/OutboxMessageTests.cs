using Clovent.Restaurant.Outbox;
using Xunit;

namespace Clovent.Restaurant.Tests;

public sealed class OutboxMessageTests
{
    [Fact]
    public void Create_InitializesMessageInPendingState()
    {
        var msg = OutboxMessage.Create(
            OutboxMessageType.InventoryPosting,
            "Order",
            Guid.NewGuid().ToString(),
            "corr-1",
            "{}",
            "idem-1");

        Assert.Equal(OutboxMessageStatus.Pending, msg.Status);
        Assert.Equal(OutboxMessageType.InventoryPosting, msg.MessageType);
        Assert.Equal("Order", msg.AggregateType);
        Assert.Equal("corr-1", msg.CorrelationId);
        Assert.Equal("idem-1", msg.IdempotencyKey);
        Assert.Equal(0, msg.AttemptCount);
    }

    [Fact]
    public void ClaimForProcessing_TransitionsToProcessing_AndIncrementsAttempts()
    {
        var msg = OutboxMessage.Create("TestType", "Order", "id-1", "corr-1", "{}");

        msg.ClaimForProcessing();

        Assert.Equal(OutboxMessageStatus.Processing, msg.Status);
        Assert.Equal(1, msg.AttemptCount);
        Assert.NotNull(msg.ProcessingStartedAtUtc);
    }

    [Fact]
    public void MarkCompleted_TransitionsToCompleted_AndSetsTimestamp()
    {
        var msg = OutboxMessage.Create("TestType", "Order", "id-1", "corr-1", "{}");
        msg.ClaimForProcessing();

        msg.MarkCompleted();

        Assert.Equal(OutboxMessageStatus.Completed, msg.Status);
        Assert.NotNull(msg.CompletedAtUtc);
    }

    [Fact]
    public void ScheduleRetry_CalculatesExponentialBackoff_UnderMaxAttempts()
    {
        var msg = OutboxMessage.Create("TestType", "Order", "id-1", "corr-1", "{}");
        msg.ClaimForProcessing();

        msg.ScheduleRetry("Transient failure", maxAttempts: 5);

        Assert.Equal(OutboxMessageStatus.RetryScheduled, msg.Status);
        Assert.Equal("Transient failure", msg.LastError);
        Assert.NotNull(msg.NextRetryAtUtc);
        Assert.True(msg.AvailableAtUtc > msg.CreatedAtUtc);
    }

    [Fact]
    public void ScheduleRetry_DeadLetters_WhenMaxAttemptsExceeded()
    {
        var msg = OutboxMessage.Create("TestType", "Order", "id-1", "corr-1", "{}");
        
        // 3 failures with maxAttempts = 3
        msg.ClaimForProcessing();
        msg.ScheduleRetry("Err 1", maxAttempts: 3);
        msg.ClaimForProcessing();
        msg.ScheduleRetry("Err 2", maxAttempts: 3);
        msg.ClaimForProcessing();
        msg.ScheduleRetry("Err 3", maxAttempts: 3);

        Assert.Equal(OutboxMessageStatus.DeadLetter, msg.Status);
        Assert.Contains("Max retry attempts (3) exceeded", msg.LastError);
    }

    [Fact]
    public void RecoverFromStaleProcessing_ResetsProcessingMessageBackToRetryScheduled()
    {
        var msg = OutboxMessage.Create("TestType", "Order", "id-1", "corr-1", "{}");
        msg.ClaimForProcessing();

        msg.RecoverFromStaleProcessing();

        Assert.Equal(OutboxMessageStatus.RetryScheduled, msg.Status);
        Assert.Contains("Worker crash recovery", msg.LastError);
    }
}
