namespace Clovent.MasterData.Departments;

/// <summary>Strongly-typed identifier for a <see cref="Department"/> aggregate.</summary>
public readonly record struct DepartmentId(Guid Value)
{
    /// <summary>The underlying value, guaranteed never to be <see cref="Guid.Empty"/>.</summary>
    public Guid Value { get; } = Value == Guid.Empty
        ? throw new ArgumentException("DepartmentId cannot be empty.", nameof(Value))
        : Value;

    /// <summary>Creates a new, unique <see cref="DepartmentId"/>.</summary>
    public static DepartmentId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
