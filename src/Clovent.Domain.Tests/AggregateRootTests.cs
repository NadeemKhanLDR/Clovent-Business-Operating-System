using Clovent.Domain.Tests.TestSupport;
using Xunit;

namespace Clovent.Domain.Tests;

public class AggregateRootTests
{
    [Fact]
    public void DomainEvents_StartsEmpty()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void AddDomainEvent_RecordsEvent()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.DoSomething();

        Assert.Single(aggregate.DomainEvents);
        Assert.IsType<TestEvent>(aggregate.DomainEvents.Single());
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllRecordedEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.DoSomething();
        aggregate.DoSomething();

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void DomainEvents_IsReadOnly()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        Assert.IsAssignableFrom<IReadOnlyCollection<Clovent.Domain.IDomainEvent>>(aggregate.DomainEvents);
    }
}
