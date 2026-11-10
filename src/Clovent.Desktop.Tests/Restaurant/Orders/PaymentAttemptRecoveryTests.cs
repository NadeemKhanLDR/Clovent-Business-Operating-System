using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Application.Payments.Commands;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public sealed class PaymentAttemptRecoveryTests
{
    [Fact]
    public async Task KnownDomainRejection_RefreshesAndAllowsCorrectedAttempt()
    {
        var recovery = new PaymentAttemptRecovery();
        var command = new RecordPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 40m, ShiftId: Guid.NewGuid());
        var refreshes = 0;
        Task Refresh() { refreshes++; return Task.CompletedTask; }
        await Assert.ThrowsAsync<Clovent.Restaurant.RestaurantDomainException>(() => recovery.ExecuteAsync(command,
            _ => throw Clovent.Restaurant.RestaurantDomainException.PaymentRequiresOpenShift(new(command.ShiftId!.Value)), Refresh));
        var sends = 0;
        await recovery.ExecuteAsync(command with { ShiftId = Guid.NewGuid() }, _ => { sends++; return Task.CompletedTask; }, Refresh);
        Assert.Equal(1, sends);
        Assert.Equal(2, refreshes);
    }

    [Fact]
    public async Task CompletionOrReceiptFailureAfterPayment_IsReturnedAsFollowUpWarning()
    {
        var calls = 0;
        var failure = await PaymentAttemptRecovery.RunCommittedFollowUpAsync(() =>
        {
            calls++;
            throw new InvalidOperationException("injected completion/receipt error");
        });
        Assert.NotNull(failure);
        Assert.Equal("injected completion/receipt error", failure.Message);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task LostAcknowledgement_RefreshesAndReusesOriginalKey()
    {
        var recovery = new PaymentAttemptRecovery();
        var command = new RecordPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 40m);
        var committed = new Dictionary<string, decimal>();
        var keys = new List<string>();
        var refreshes = 0;
        Task Commit(RecordPaymentCommand request)
        {
            keys.Add(request.IdempotencyKey!);
            committed.TryAdd(request.IdempotencyKey!, request.Amount);
            if (keys.Count == 1) throw new TimeoutException("lost acknowledgement after commit");
            return Task.CompletedTask;
        }
        Task Refresh() { refreshes++; return Task.CompletedTask; }
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => recovery.ExecuteAsync(command, Commit, Refresh));
        Assert.Contains("unconfirmed", error.Message);
        await recovery.ExecuteAsync(command, Commit, Refresh);
        Assert.Equal(keys[0], keys[1]);
        Assert.Equal(40m, Assert.Single(committed).Value);
        Assert.Equal(2, refreshes);
        await recovery.ExecuteAsync(command, Commit, Refresh);
        Assert.NotEqual(keys[1], keys[2]); // a new intentional, acknowledged attempt
    }

    [Fact]
    public async Task PendingAttempt_RejectsChangedDetailsWithoutSending()
    {
        var recovery = new PaymentAttemptRecovery();
        var command = new RecordPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 40m);
        var sends = 0;
        Task Commit(RecordPaymentCommand _) { sends++; throw new TimeoutException(); }
        await Assert.ThrowsAsync<InvalidOperationException>(() => recovery.ExecuteAsync(command, Commit, () => Task.CompletedTask));
        await Assert.ThrowsAsync<InvalidOperationException>(() => recovery.ExecuteAsync(command with { Amount = 60m }, Commit, () => Task.CompletedTask));
        Assert.Equal(1, sends);
    }

    [Fact]
    public async Task RefreshFailureAfterAcknowledgedCommit_ReportsSavedAndRetainsKey()
    {
        var recovery = new PaymentAttemptRecovery();
        var command = new RecordPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 100m);
        var keys = new List<string>();
        Task Commit(RecordPaymentCommand request) { keys.Add(request.IdempotencyKey!); return Task.CompletedTask; }
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => recovery.ExecuteAsync(command, Commit, () => throw new TimeoutException()));
        Assert.Contains("Payment is saved", error.Message);
        await recovery.ExecuteAsync(command, Commit, () => Task.CompletedTask);
        Assert.Equal(keys[0], keys[1]);
    }
}
