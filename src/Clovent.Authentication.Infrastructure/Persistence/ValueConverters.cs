using System.Text.Json;
using Clovent.Authentication.Credentials;
using Clovent.Authentication.LoginAttempts;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Clovent.Authentication.Shared.ValueObjects;
using Clovent.Identity.Users;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Clovent.Authentication.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="ValueConverter{TModel,TProvider}"/>s shared across the
/// entity type configurations in this project. Every strongly-typed
/// identifier and value object in the Authentication Domain is opaque to SQL
/// Server unless it's converted to/from a primitive column type - these
/// converters do that without ever changing the Domain project itself, using
/// only each type's already-public factory methods
/// (<c>PasswordHash.Create</c>, <c>FailedAttempts.Increment</c>, etc.).
/// </summary>
internal static class ValueConverters
{
    /// <summary><see cref="SessionId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<SessionId, Guid> SessionIdConverter =
        new(id => id.Value, value => new SessionId(value));

    /// <summary><see cref="UserId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<UserId, Guid> UserIdConverter =
        new(id => id.Value, value => new UserId(value));

    /// <summary><see cref="LoginAttemptId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<LoginAttemptId, Guid> LoginAttemptIdConverter =
        new(id => id.Value, value => new LoginAttemptId(value));

    /// <summary>Nullable <see cref="UserId"/> &lt;-&gt; nullable <see cref="Guid"/>, for <c>LoginAttempt.UserId</c> (only resolved once the identifier matches a real user).</summary>
    public static readonly ValueConverter<UserId?, Guid?> NullableUserIdConverter =
        new(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new UserId(value.Value) : null);

    /// <summary><see cref="RefreshSessionId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<RefreshSessionId, Guid> RefreshSessionIdConverter =
        new(id => id.Value, value => new RefreshSessionId(value));

    /// <summary><see cref="UserCredentialsId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<UserCredentialsId, Guid> UserCredentialsIdConverter =
        new(id => id.Value, value => new UserCredentialsId(value));

    /// <summary>Nullable <see cref="IpAddress"/> &lt;-&gt; nullable normalized address string.</summary>
    public static readonly ValueConverter<IpAddress?, string?> IpAddressConverter =
        new(v => v == null ? null : v.Value, v => v == null ? null : IpAddress.Create(v));

    /// <summary>
    /// <see cref="TimeSpan"/> &lt;-&gt; ticks (<see cref="long"/>), rather than SQL Server's native
    /// <c>time</c> column type - <c>time</c> cannot represent a duration of 24 hours or more, and
    /// nothing here guarantees an idle timeout or refresh lifetime stays under that.
    /// </summary>
    public static readonly ValueConverter<TimeSpan, long> TimeSpanTicksConverter =
        new(v => v.Ticks, v => TimeSpan.FromTicks(v));

    /// <summary>Nullable <see cref="PasswordHash"/> &lt;-&gt; nullable opaque hash string.</summary>
    public static readonly ValueConverter<PasswordHash?, string?> PasswordHashConverter =
        new(v => v == null ? null : v.Value, v => v == null ? null : PasswordHash.Create(v));

    /// <summary>Nullable <see cref="PinHash"/> &lt;-&gt; nullable opaque hash string.</summary>
    public static readonly ValueConverter<PinHash?, string?> PinHashConverter =
        new(v => v == null ? null : v.Value, v => v == null ? null : PinHash.Create(v));

    /// <summary><see cref="SecurityStamp"/> &lt;-&gt; opaque marker string.</summary>
    public static readonly ValueConverter<SecurityStamp, string> SecurityStampConverter =
        new(v => v.Value, v => SecurityStamp.Create(v));

    /// <summary>
    /// <see cref="Credentials.FailedAttempts"/> &lt;-&gt; its consecutive-failure count.
    /// <see cref="Credentials.FailedAttempts"/> exposes no factory that takes an arbitrary count
    /// directly (only <c>Zero</c> and <c>Increment</c>, one at a time, by design - see
    /// <c>AuthenticationDomain.md</c>) so reconstruction replays <c>Increment()</c> the stored
    /// number of times from <c>Zero</c>; counts here are small (bounded by a lockout threshold),
    /// so this is cheap and requires no change to the Domain project.
    /// </summary>
    public static readonly ValueConverter<FailedAttempts, int> FailedAttemptsConverter =
        new(v => v.Count, v => RestoreFailedAttempts(v));

    /// <summary>
    /// <see cref="Credentials.PasswordHistory"/> &lt;-&gt; a JSON array of its entries. The history is
    /// capped at a handful of entries, never queried independently of its owning
    /// <see cref="UserCredentials"/>, and reconstructing it does not require an owned collection
    /// table - a single column keeps the mapping simple without changing the Domain project.
    /// Reconstruction replays the already-public <c>WithNewPassword</c> from oldest to newest, the
    /// same way the aggregate itself built the history originally.
    /// </summary>
    public static readonly ValueConverter<PasswordHistory, string> PasswordHistoryConverter =
        new(
            v => JsonSerializer.Serialize(v.Entries.Select(e => new PasswordHistoryEntryDto(e.Hash.Value, e.ChangedAtUtc)), JsonOptions),
            v => RestorePasswordHistory(JsonSerializer.Deserialize<List<PasswordHistoryEntryDto>>(v, JsonOptions) ?? new List<PasswordHistoryEntryDto>()));

    private static readonly JsonSerializerOptions JsonOptions = new();

    private static FailedAttempts RestoreFailedAttempts(int count)
    {
        var result = FailedAttempts.Zero;
        for (var i = 0; i < count; i++)
            result = result.Increment();

        return result;
    }

    private static PasswordHistory RestorePasswordHistory(List<PasswordHistoryEntryDto> entries)
    {
        var history = PasswordHistory.Empty;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            history = history.WithNewPassword(PasswordHash.Create(entry.Hash), entry.ChangedAtUtc, PasswordHistory.DefaultMaxSize);
        }

        return history;
    }

    private sealed record PasswordHistoryEntryDto(string Hash, DateTimeOffset ChangedAtUtc);
}
