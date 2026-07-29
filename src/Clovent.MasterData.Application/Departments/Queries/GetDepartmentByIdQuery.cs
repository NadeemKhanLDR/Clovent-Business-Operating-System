using Clovent.MasterData.Application.Departments.Dtos;
using Clovent.MasterData.Departments;
using MediatR;

namespace Clovent.MasterData.Application.Departments.Queries;

/// <summary>Retrieves a single department by identity.</summary>
public sealed record GetDepartmentByIdQuery(Guid DepartmentId) : IRequest<DepartmentDto>;

/// <summary>Handles <see cref="GetDepartmentByIdQuery"/>.</summary>
public sealed class GetDepartmentByIdQueryHandler(IDepartmentRepository departmentRepository)
    : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto>
{
    /// <inheritdoc/>
    public async Task<DepartmentDto> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetByIdAsync(new DepartmentId(request.DepartmentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Department), request.DepartmentId);

        return DepartmentDto.FromDomain(department);
    }
}
