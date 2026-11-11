using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clovent.Restaurant.Application.Payments.Commands;

namespace Clovent.Desktop.Restaurant.Orders;

internal interface IPaymentAttemptJournal
{
    string OwnerIdentity { get; }
    Task<RecordPaymentCommand> GetOrCreateAsync(RecordPaymentCommand candidate);
    Task ClearAsync(Guid orderId, string key);
}

/// <summary>Versioned, user-protected attempt identity; never stores manager approval.</summary>
internal sealed class ProtectedPaymentAttemptJournal(string directory, string terminal, Func<Guid?> cashier) : IPaymentAttemptJournal
{
    private sealed record Envelope(int Version, string Owner, Guid OrderId, Guid MethodId, decimal Amount, Guid? ShiftId, string Key);
    public string OwnerIdentity => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        terminal + ":" + (cashier() ?? throw new InvalidOperationException("An authenticated cashier is required to preserve a payment attempt.")))));

    private string PathFor(Guid orderId) => Path.Combine(directory, $"attempt-{OwnerIdentity}-{orderId:N}.bin");

    private static async Task<FileStream> LockAsync(string path)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (true)
        {
            try { return new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (!timeout.IsCancellationRequested) { await Task.Delay(25, timeout.Token); }
        }
    }

    private async Task<Envelope?> ReadAsync(string path, Guid orderId)
    {
        if (!File.Exists(path)) return null;
        var plain = ProtectedData.Unprotect(await File.ReadAllBytesAsync(path), null, DataProtectionScope.CurrentUser);
        var value = JsonSerializer.Deserialize<Envelope>(plain) ?? throw new InvalidDataException("Payment attempt journal is empty.");
        if (value.Version != 1 || value.Owner != OwnerIdentity || value.OrderId != orderId || string.IsNullOrWhiteSpace(value.Key))
            throw new InvalidDataException("Payment attempt journal version or owner is invalid. Preserve it and reconcile payment history before retrying.");
        return value;
    }

    public async Task<RecordPaymentCommand> GetOrCreateAsync(RecordPaymentCommand candidate)
    {
        var owner = OwnerIdentity;
        Directory.CreateDirectory(directory);
        var path = PathFor(candidate.OrderId);
        await using var gate = await LockAsync(path);
        var existing = await ReadAsync(path, candidate.OrderId);
        if (existing is not null)
            return new(existing.OrderId, existing.MethodId, existing.Amount, false, existing.ShiftId, existing.Key);
        var value = new Envelope(1, owner, candidate.OrderId, candidate.PaymentMethodId, candidate.Amount, candidate.ShiftId, Guid.NewGuid().ToString("N"));
        var encrypted = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(value), null, DataProtectionScope.CurrentUser);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(encrypted);
                await stream.FlushAsync();
                stream.Flush(flushToDisk: true);
            }
            if (OwnerIdentity != owner) throw new InvalidOperationException("Cashier changed before preserving the payment attempt. No payment was sent.");
            File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return candidate with { IdempotencyKey = value.Key, ExceedCreditLimitApproved = false };
    }

    public async Task ClearAsync(Guid orderId, string key)
    {
        var path = PathFor(orderId);
        if (!File.Exists(path)) return;
        await using var gate = await LockAsync(path);
        var existing = await ReadAsync(path, orderId);
        if (existing is not null && existing.Key != key) throw new InvalidOperationException("Another payment attempt owns this journal. Preserve it for reconciliation.");
        File.Delete(path);
    }
}
