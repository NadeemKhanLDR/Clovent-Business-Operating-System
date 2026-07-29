namespace Clovent.Domain;

/// <summary>
/// An <see cref="Entity{TId}"/> that is the single entry point for changes
/// within its consistency boundary. Only aggregate roots get repositories;
/// everything reachable only through a root is modified through the root's
/// own methods, which are responsible for enforcing that aggregate's
/// invariants on every state change.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for this aggregate.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Events raised by this instance since it was created or last cleared.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Records that a business-significant state change happened. Only the
    /// aggregate itself may raise its own events - callers observe them
    /// afterward via <see cref="DomainEvents"/>, they never add one directly.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Discards recorded events, typically called by whatever dispatches
    /// them (e.g. after persistence commits) once they've been handled.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
