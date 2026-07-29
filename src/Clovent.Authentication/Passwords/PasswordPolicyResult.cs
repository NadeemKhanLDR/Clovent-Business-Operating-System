namespace Clovent.Authentication.Passwords;

/// <summary>The outcome of evaluating a candidate password against a <see cref="PasswordPolicy"/>.</summary>
public sealed class PasswordPolicyResult
{
    /// <summary><see langword="true"/> when the candidate violates no rule.</summary>
    public bool IsSatisfied => Violations.Count == 0;

    /// <summary>Human-readable descriptions of every rule the candidate violated. Empty when satisfied.</summary>
    public IReadOnlyList<string> Violations { get; }

    internal PasswordPolicyResult(IReadOnlyList<string> violations) => Violations = violations;
}
