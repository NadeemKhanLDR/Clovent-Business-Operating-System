using Clovent.Identity.Branches;
using Clovent.MasterData.Application.Departments.Dtos;
using Clovent.MasterData.Departments;
using MediatR;

namespace Clovent.MasterData.Application.Departments.Queries;

/// <summary>Retrieves every department belonging to the given branch.</summary>
public sealed record ListDepartmentsByBranchQuery(Guid BranchId) : IRequest<IReadOnlyCollection<DepartmentDto>>;

/// <summary>Handles <see cref="ListDepartmentsByBranchQuery"/>.</summary>
public sealed class ListDepartmentsByBranchQueryHandler(IDepartmentRepository departmentRepository)
    : IRequestHandler<ListDepartmentsByBranchQuery, IReadOnlyCollection<DepartmentDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<DepartmentDto>> Handle(ListDepartmentsByBranchQuery request, CancellationToken cancellationToken)
    {
        var departments = await departmentRepository.GetByBranchIdAsync(new BranchId(request.BranchId), cancellationToken);
        return [.. departments.Select(DepartmentDto.FromDomain)];
    }
}
