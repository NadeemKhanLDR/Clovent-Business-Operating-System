namespace Clovent.Authentication.Application;

/// <summary>
/// Raised by a command/query handler when the aggregate a request targets
/// does not exist. An Application-layer concern (the request referenced a
/// missing identity), distinct from <c>AuthenticationDomainException</c>
/// (an existing aggregate's own invariant was violated).
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Raised when the aggregate a request targets does not exist.</summary>
    public NotFoundException(string aggregateName, object id)
        : base($"{aggregateName} '{id}' was not found.")
    {
    }
}
