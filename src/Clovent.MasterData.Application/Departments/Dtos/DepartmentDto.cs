using Clovent.MasterData.Departments;

namespace Clovent.MasterData.Application.Departments.Dtos;

/// <summary>Read-model shape for a <see cref="Department"/>, safe to cross a process boundary.</summary>
public sealed record DepartmentDto(
    Guid DepartmentId,
    Guid BranchId,
    string Name,
    string Status,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Department"/> into its DTO.</summary>
    public static DepartmentDto FromDomain(Department department) => new(
        department.Id.Value,
        department.BranchId.Value,
        department.Name.Value,
        department.Status.ToString(),
        department.CreatedAtUtc);
}
