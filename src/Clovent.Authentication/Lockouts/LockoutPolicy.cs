using Clovent.Domain;

namespace Clovent.Authentication.Lockouts;

/// <summary>
/// Business rule for whether repeated failed login attempts should trigger
/// an account lockout. Deliberately pure - it takes an already-computed
/// failure count rather than querying <c>ILoginAttemptRepository</c> itself,
/// so counting attempts within <see cref="EvaluationWindow"/> and acting on
/// the result (calling <c>Clovent.Identity.Users.User.Lock()</c>) are both
/// Application-layer orchestration concerns.
/// </summary>
public sealed class LockoutPolicy : ValueObject
{
    /// <summary>The number of failed attempts within <see cref="EvaluationWindow"/> that triggers a lockout.</summary>
    public int MaxFailedAttempts { get; }

    /// <summary>The trailing time window over which failed attempts are counted.</summary>
    public TimeSpan EvaluationWindow { get; }

    private LockoutPolicy(int maxFailedAttempts, TimeSpan evaluationWindow)
    {
        MaxFailedAttempts = maxFailedAttempts;
        EvaluationWindow = evaluationWindow;
    }

    /// <summary>The organization-wide default policy: lock after 5 failed attempts within 15 minutes.</summary>
    public static LockoutPolicy Default { get; } = new(maxFailedAttempts: 5, evaluationWindow: TimeSpan.FromMinutes(15));

    /// <summary>Defines a custom lockout policy.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxFailedAttempts"/> or <paramref name="evaluationWindow"/> is not positive.</exception>
    public static LockoutPolicy Create(int maxFailedAttempts, TimeSpan evaluationWindow)
    {
        if (maxFailedAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxFailedAttempts), maxFailedAttempts, "Max failed attempts must be positive.");
        if (evaluationWindow <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(evaluationWindow), evaluationWindow, "Evaluation window must be positive.");

        return new LockoutPolicy(maxFailedAttempts, evaluationWindow);
    }

    /// <summary>Whether <paramref name="recentFailedAttemptCount"/> failures within the window should trigger a lockout.</summary>
    public bool ShouldLock(int recentFailedAttemptCount) => recentFailedAttemptCount >= MaxFailedAttempts;

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MaxFailedAttempts;
        yield return EvaluationWindow;
    }
}
