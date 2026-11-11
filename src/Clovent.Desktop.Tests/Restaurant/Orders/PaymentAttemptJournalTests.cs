using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Application.Payments.Commands;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public sealed class PaymentAttemptJournalTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"cbos-payment-journal-{Guid.NewGuid():N}");
    private readonly Guid _cashier = Guid.NewGuid();
    private ProtectedPaymentAttemptJournal Journal() => new(_directory, "SYNTHETIC-TERMINAL", () => _cashier);

    [Fact]
    public async Task RestartAfterLostAcknowledgement_ReusesKeyForPartialPayment()
    {
        var command = new RecordPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 40m);
        var payments = new Dictionary<string, decimal>();
        Task Commit(RecordPaymentCommand request)
        {
            payments.TryAdd(request.IdempotencyKey!, request.Amount);
            if (payments.Count == 1 && payments.Values.Sum() == 40m && !acknowledge) throw new TimeoutException("response lost after partial payment");
            return Task.CompletedTask;
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => new PaymentAttemptRecovery(Journal()).ExecuteAsync(command, Commit, () => Task.CompletedTask));
        acknowledge = true;
        await new PaymentAttemptRecovery(Journal()).ExecuteAsync(command, Commit, () => Task.CompletedTask);
        Assert.Single(payments);
        Assert.Equal(40m, payments.Values.Sum());
    }
    private bool acknowledge;

    [Fact]
    public async Task ConcurrentCoordinators_PreserveOneKey_AndRejectChangedPayloadAfterRestart()
    {
        var command = new RecordPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 40m);
        var attempts = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Journal().GetOrCreateAsync(command)));
        Assert.Single(attempts.Select(a => a.IdempotencyKey).Distinct());
        var sends = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => new PaymentAttemptRecovery(Journal()).ExecuteAsync(command with { Amount = 50m },
            _ => { sends++; return Task.CompletedTask; }, () => Task.CompletedTask));
        Assert.Equal(0, sends);
        Assert.Single(Directory.GetFiles(_directory, "*.bin"));
    }

    [Fact]
    public async Task StorageFailure_PreventsSendingPayment()
    {
        await File.WriteAllTextAsync(_directory, "occupied path");
        var sends = 0;
        try
        {
            await Assert.ThrowsAnyAsync<IOException>(() => new PaymentAttemptRecovery(Journal()).ExecuteAsync(
                new(Guid.NewGuid(), Guid.NewGuid(), 40m), _ => { sends++; return Task.CompletedTask; }, () => Task.CompletedTask));
            Assert.Equal(0, sends);
        }
        finally { File.Delete(_directory); }
    }

    [Fact]
    public async Task Journal_IsDurableButDoesNotRestoreManagerApproval()
    {
        var command = new RecordPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 40m, ExceedCreditLimitApproved: true);
        var saved = await Journal().GetOrCreateAsync(command);
        var restored = await Journal().GetOrCreateAsync(command);
        Assert.Equal(saved.IdempotencyKey, restored.IdempotencyKey);
        Assert.False(saved.ExceedCreditLimitApproved);
        Assert.False(restored.ExceedCreditLimitApproved);
        await Journal().ClearAsync(command.OrderId, restored.IdempotencyKey!);
        Assert.Empty(Directory.GetFiles(_directory, "*.bin"));
    }

    [Fact]
    public async Task CorruptJournal_FailsBeforeSendingAndIsPreserved()
    {
        var command = new RecordPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), 40m);
        await Journal().GetOrCreateAsync(command);
        var path = Assert.Single(Directory.GetFiles(_directory, "*.bin"));
        await File.WriteAllBytesAsync(path, [1, 2, 3]);
        var sends = 0;
        await Assert.ThrowsAsync<System.Security.Cryptography.CryptographicException>(() => new PaymentAttemptRecovery(Journal()).ExecuteAsync(command,
            _ => { sends++; return Task.CompletedTask; }, () => Task.CompletedTask));
        Assert.Equal(0, sends);
        Assert.True(File.Exists(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory) && Path.GetFullPath(_directory).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
            Directory.Delete(_directory, recursive: true);
    }
}
