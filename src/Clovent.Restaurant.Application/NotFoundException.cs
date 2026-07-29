namespace Clovent.Restaurant.Application;

/// <summary>Raised by a command/query handler when the aggregate a request targets does not exist. Mirrors <c>Clovent.Catalog.Application.NotFoundException</c>.</summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Raised when the aggregate a request targets does not exist.</summary>
    public NotFoundException(string aggregateName, object id)
        : base($"{aggregateName} '{id}' was not found.")
    {
    }
}
