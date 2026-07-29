using Clovent.Domain;

namespace Clovent.Authentication.Credentials;

/// <summary>
/// A denormalized consecutive-failure counter for a credential - a cheap
/// check that doesn't require querying <c>LoginAttempt</c> history, complementary
/// to <see cref="Lockouts.LockoutPolicy"/>'s window-based evaluation over
/// that history. Increments on each failure, resets to zero on success.
/// </summary>
public sealed class FailedAttempts : ValueObject
{
    /// <summary>The current consecutive-failure count.</summary>
    public int Count { get; }

    private FailedAttempts(int count) => Count = count;

    /// <summary>No recorded failures.</summary>
    public static FailedAttempts Zero { get; } = new(0);

    /// <summary>Returns the count incremented by one, recording another failure.</summary>
    public FailedAttempts Increment() => new(Count + 1);

    /// <summary>Returns the count reset to zero, e.g. after a successful authentication.</summary>
    public FailedAttempts Reset() => Zero;

    /// <summary>Whether the count has reached or exceeded <paramref name="threshold"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="threshold"/> is not positive.</exception>
    public bool MeetsOrExceeds(int threshold)
    {
        if (threshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Threshold must be positive.");

        return Count >= threshold;
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Count;
    }
}
