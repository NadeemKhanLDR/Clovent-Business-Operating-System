namespace Clovent.Identity.Application;

/// <summary>
/// Raised by a command/query handler when the aggregate a request targets
/// does not exist. An Application-layer concern, distinct from
/// <c>IdentityDomainException</c> (an existing aggregate's own invariant
/// was violated). Mirrors <c>Clovent.Authentication.Application.NotFoundException</c>.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Raised when the aggregate a request targets does not exist.</summary>
    public NotFoundException(string aggregateName, object id)
        : base($"{aggregateName} '{id}' was not found.")
    {
    }
}
