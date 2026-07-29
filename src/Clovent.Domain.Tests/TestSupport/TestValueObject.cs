using Clovent.Domain;

namespace Clovent.Domain.Tests.TestSupport;

internal sealed class TestValueObject : ValueObject
{
    public string First { get; }
    public int Second { get; }

    public TestValueObject(string first, int second)
    {
        First = first;
        Second = second;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return First;
        yield return Second;
    }
}

internal sealed class OtherTestValueObject : ValueObject
{
    public string First { get; }

    public OtherTestValueObject(string first) => First = first;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return First;
    }
}
