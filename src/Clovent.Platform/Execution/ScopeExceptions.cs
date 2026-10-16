namespace Clovent.Platform.Execution;

/// <summary>
/// Thrown when an operation requiring execution scope is dispatched without a valid, resolved
/// execution context. Null or unassigned scope never grants unrestricted global access.
/// </summary>
public class ScopeUnresolvedException : UnauthorizedAccessException
{
    /// <summary>Initializes a new instance of <see cref="ScopeUnresolvedException"/> with an error message.</summary>
    public ScopeUnresolvedException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="ScopeUnresolvedException"/> with an error message and inner exception.</summary>
    public ScopeUnresolvedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when an operation targets an organization, company, or branch that conflicts with
/// the caller's active, verified execution scope.
/// </summary>
public class ScopeMismatchException : UnauthorizedAccessException
{
    /// <summary>Initializes a new instance of <see cref="ScopeMismatchException"/> with an error message.</summary>
    public ScopeMismatchException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="ScopeMismatchException"/> with an error message and inner exception.</summary>
    public ScopeMismatchException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when authoritative ownership validation encounters an inconsistent or broken
/// tenancy hierarchy (e.g. terminal does not belong to branch, branch does not belong to company).
/// </summary>
public class ScopeConsistencyException : InvalidOperationException
{
    /// <summary>Initializes a new instance of <see cref="ScopeConsistencyException"/> with an error message.</summary>
    public ScopeConsistencyException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="ScopeConsistencyException"/> with an error message and inner exception.</summary>
    public ScopeConsistencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
