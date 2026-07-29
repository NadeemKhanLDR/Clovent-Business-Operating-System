namespace Clovent.Authentication.Pins;

/// <summary>The outcome of evaluating a candidate PIN against a <see cref="PinPolicy"/>.</summary>
public sealed class PinPolicyResult
{
    /// <summary><see langword="true"/> when the candidate violates no rule.</summary>
    public bool IsSatisfied => Violations.Count == 0;

    /// <summary>Human-readable descriptions of every rule the candidate violated. Empty when satisfied.</summary>
    public IReadOnlyList<string> Violations { get; }

    internal PinPolicyResult(IReadOnlyList<string> violations) => Violations = violations;
}
