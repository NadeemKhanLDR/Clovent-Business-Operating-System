using Clovent.Catalog.UnitsOfMeasure;

namespace Clovent.Catalog.Application.UnitsOfMeasure.Dtos;

/// <summary>Read-model shape for a <see cref="UnitOfMeasure"/>, safe to cross a process boundary.</summary>
public sealed record UnitOfMeasureDto(Guid UnitOfMeasureId, string Code, string Name, string Status, DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="UnitOfMeasure"/> into its DTO.</summary>
    public static UnitOfMeasureDto FromDomain(UnitOfMeasure unit) => new(
        unit.Id.Value, unit.Code.Value, unit.Name, unit.Status.ToString(), unit.CreatedAtUtc);
}
