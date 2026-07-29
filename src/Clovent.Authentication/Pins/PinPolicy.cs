using Clovent.Domain;

namespace Clovent.Authentication.Pins;

/// <summary>
/// Business rules a candidate PIN must satisfy. Evaluates shape and strength
/// only - it never sees, stores, or hashes an actual credential; that is an
/// Infrastructure concern for a later milestone.
/// </summary>
public sealed class PinPolicy : ValueObject
{
    /// <summary>The shortest permitted digit count.</summary>
    public int MinLength { get; }

    /// <summary>The longest permitted digit count.</summary>
    public int MaxLength { get; }

    /// <summary>Whether a PIN of a single repeated digit (e.g. "1111") is rejected.</summary>
    public bool DisallowRepeatedDigits { get; }

    /// <summary>Whether a strictly ascending or descending run (e.g. "1234", "4321") is rejected.</summary>
    public bool DisallowSequentialDigits { get; }

    private PinPolicy(int minLength, int maxLength, bool disallowRepeatedDigits, bool disallowSequentialDigits)
    {
        MinLength = minLength;
        MaxLength = maxLength;
        DisallowRepeatedDigits = disallowRepeatedDigits;
        DisallowSequentialDigits = disallowSequentialDigits;
    }

    /// <summary>The organization-wide default policy: 4-6 digits, rejecting repeated and sequential runs.</summary>
    public static PinPolicy Default { get; } = new(
        minLength: 4,
        maxLength: 6,
        disallowRepeatedDigits: true,
        disallowSequentialDigits: true);

    /// <summary>Defines a custom PIN policy.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> is not positive, or exceeds <paramref name="maxLength"/>.</exception>
    public static PinPolicy Create(int minLength, int maxLength, bool disallowRepeatedDigits, bool disallowSequentialDigits)
    {
        if (minLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Minimum length must be positive.");
        if (maxLength < minLength)
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Maximum length cannot be less than minimum length.");

        return new PinPolicy(minLength, maxLength, disallowRepeatedDigits, disallowSequentialDigits);
    }

    /// <summary>Evaluates <paramref name="candidate"/> against every rule in this policy.</summary>
    public PinPolicyResult Evaluate(string candidate)
    {
        candidate ??= string.Empty;
        var violations = new List<string>();

        if (candidate.Length < MinLength || candidate.Length > MaxLength)
            violations.Add($"PIN must be {MinLength}-{MaxLength} digits long.");
        if (!candidate.All(char.IsDigit))
            violations.Add("PIN must contain digits only.");
        if (DisallowRepeatedDigits && candidate.Length > 0 && candidate.Distinct().Count() == 1)
            violations.Add("PIN cannot be a single repeated digit.");
        if (DisallowSequentialDigits && IsSequential(candidate))
            violations.Add("PIN cannot be a sequential run of digits.");

        return new PinPolicyResult(violations);
    }

    private static bool IsSequential(string candidate)
    {
        if (candidate.Length < 2 || !candidate.All(char.IsDigit))
            return false;

        var ascending = true;
        var descending = true;

        for (var i = 1; i < candidate.Length; i++)
        {
            var previous = candidate[i - 1] - '0';
            var current = candidate[i] - '0';

            if (current != previous + 1) ascending = false;
            if (current != previous - 1) descending = false;
        }

        return ascending || descending;
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinLength;
        yield return MaxLength;
        yield return DisallowRepeatedDigits;
        yield return DisallowSequentialDigits;
    }
}
