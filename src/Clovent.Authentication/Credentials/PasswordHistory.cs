using Clovent.Domain;

namespace Clovent.Authentication.Credentials;

/// <summary>
/// The trailing set of previously-used password hashes for a credential,
/// most recent first, capped at a maximum size - the data a "cannot reuse
/// your last N passwords" rule is checked against. Also the source of truth
/// for "when was the password last changed" (<see cref="LastChangedAtUtc"/>):
/// deriving it from the most recent entry avoids tracking the same fact in
/// two places that could drift out of sync.
/// </summary>
public sealed class PasswordHistory : ValueObject
{
    /// <summary>The default cap on how many prior passwords are retained.</summary>
    public const int DefaultMaxSize = 5;

    /// <summary>Prior passwords, most recently changed first.</summary>
    public IReadOnlyList<PasswordHistoryEntry> Entries { get; }

    /// <summary>UTC instant the current (most recent) password was set, or <see langword="null"/> if the history is empty.</summary>
    public DateTimeOffset? LastChangedAtUtc => Entries.Count > 0 ? Entries[0].ChangedAtUtc : null;

    private PasswordHistory(IReadOnlyList<PasswordHistoryEntry> entries) => Entries = entries;

    /// <summary>A history with no recorded passwords.</summary>
    public static PasswordHistory Empty { get; } = new([]);

    /// <summary>
    /// Returns a new history with <paramref name="hash"/> recorded as the
    /// most recent password, trimmed to at most <paramref name="maxSize"/> entries.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSize"/> is not positive.</exception>
    public PasswordHistory WithNewPassword(PasswordHash hash, DateTimeOffset changedAtUtc, int maxSize = DefaultMaxSize)
    {
        ArgumentNullException.ThrowIfNull(hash);
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize), maxSize, "Max history size must be positive.");

        var updated = new List<PasswordHistoryEntry> { new(hash, changedAtUtc) };
        updated.AddRange(Entries.Take(maxSize - 1));
        return new PasswordHistory(updated);
    }

    /// <summary>Whether <paramref name="candidate"/> matches any hash currently retained in this history.</summary>
    public bool Contains(PasswordHash candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return Entries.Any(e => e.Hash == candidate);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var entry in Entries)
            yield return entry;
    }
}
