using Clovent.Domain;

namespace Clovent.Domain.Tests.TestSupport;

internal sealed record TestEvent(Guid AggregateId, DateTimeOffset OccurredOnUtc) : IDomainEvent;

internal sealed class TestAggregate : AggregateRoot<Guid>
{
    public TestAggregate(Guid id) => Id = id;

    public void DoSomething() => AddDomainEvent(new TestEvent(Id, DateTimeOffset.UtcNow));
}
