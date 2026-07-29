namespace Clovent.Domain;

/// <summary>
/// Base type for failures caused by violating a domain invariant or business
/// rule (as opposed to structural/input validation, which value objects and
/// method guard clauses raise as <see cref="ArgumentException"/>). Each
/// domain module is expected to derive its own exception type from this one.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>Creates the exception with a message describing which invariant was violated.</summary>
    protected DomainException(string message) : base(message)
    {
    }
}
