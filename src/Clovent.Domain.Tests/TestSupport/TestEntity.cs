using Clovent.Domain;

namespace Clovent.Domain.Tests.TestSupport;

internal sealed class TestEntity : Entity<Guid>
{
    public TestEntity(Guid id) => Id = id;
}

internal sealed class OtherTestEntity : Entity<Guid>
{
    public OtherTestEntity(Guid id) => Id = id;
}
