using Clovent.Domain;

namespace Clovent.Authentication.Passwords;

/// <summary>
/// Business rules a candidate password must satisfy. Evaluates shape and
/// strength only - it never sees, stores, or hashes an actual credential;
/// that is an Infrastructure concern for a later milestone.
/// </summary>
public sealed class PasswordPolicy : ValueObject
{
    /// <summary>The shortest permitted length.</summary>
    public int MinLength { get; }

    /// <summary>The longest permitted length.</summary>
    public int MaxLength { get; }

    /// <summary>Whether at least one uppercase letter is required.</summary>
    public bool RequireUppercase { get; }

    /// <summary>Whether at least one lowercase letter is required.</summary>
    public bool RequireLowercase { get; }

    /// <summary>Whether at least one digit is required.</summary>
    public bool RequireDigit { get; }

    /// <summary>Whether at least one non-alphanumeric character is required.</summary>
    public bool RequireSpecialCharacter { get; }

    private PasswordPolicy(int minLength, int maxLength, bool requireUppercase, bool requireLowercase, bool requireDigit, bool requireSpecialCharacter)
    {
        MinLength = minLength;
        MaxLength = maxLength;
        RequireUppercase = requireUppercase;
        RequireLowercase = requireLowercase;
        RequireDigit = requireDigit;
        RequireSpecialCharacter = requireSpecialCharacter;
    }

    /// <summary>The organization-wide default policy: 8-128 characters, requiring upper, lower, digit, and special character.</summary>
    public static PasswordPolicy Default { get; } = new(
        minLength: 8,
        maxLength: 128,
        requireUppercase: true,
        requireLowercase: true,
        requireDigit: true,
        requireSpecialCharacter: true);

    /// <summary>Defines a custom password policy.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> is not positive, or exceeds <paramref name="maxLength"/>.</exception>
    public static PasswordPolicy Create(int minLength, int maxLength, bool requireUppercase, bool requireLowercase, bool requireDigit, bool requireSpecialCharacter)
    {
        if (minLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(minLength), minLength, "Minimum length must be positive.");
        if (maxLength < minLength)
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Maximum length cannot be less than minimum length.");

        return new PasswordPolicy(minLength, maxLength, requireUppercase, requireLowercase, requireDigit, requireSpecialCharacter);
    }

    /// <summary>Evaluates <paramref name="candidate"/> against every rule in this policy.</summary>
    public PasswordPolicyResult Evaluate(string candidate)
    {
        candidate ??= string.Empty;
        var violations = new List<string>();

        if (candidate.Length < MinLength)
            violations.Add($"Password must be at least {MinLength} characters long.");
        if (candidate.Length > MaxLength)
            violations.Add($"Password cannot exceed {MaxLength} characters.");
        if (RequireUppercase && !candidate.Any(char.IsUpper))
            violations.Add("Password must contain at least one uppercase letter.");
        if (RequireLowercase && !candidate.Any(char.IsLower))
            violations.Add("Password must contain at least one lowercase letter.");
        if (RequireDigit && !candidate.Any(char.IsDigit))
            violations.Add("Password must contain at least one digit.");
        if (RequireSpecialCharacter && candidate.All(char.IsLetterOrDigit))
            violations.Add("Password must contain at least one special character.");

        return new PasswordPolicyResult(violations);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinLength;
        yield return MaxLength;
        yield return RequireUppercase;
        yield return RequireLowercase;
        yield return RequireDigit;
        yield return RequireSpecialCharacter;
    }
}
