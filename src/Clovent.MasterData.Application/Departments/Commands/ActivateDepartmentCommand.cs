using Clovent.MasterData.Application.Departments.Dtos;
using Clovent.MasterData.Departments;
using MediatR;

namespace Clovent.MasterData.Application.Departments.Commands;

/// <summary>Activates a department.</summary>
public sealed record ActivateDepartmentCommand(Guid DepartmentId) : IRequest<DepartmentDto>;

/// <summary>Handles <see cref="ActivateDepartmentCommand"/>.</summary>
public sealed class ActivateDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    : IRequestHandler<ActivateDepartmentCommand, DepartmentDto>
{
    /// <inheritdoc/>
    public async Task<DepartmentDto> Handle(ActivateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetByIdAsync(new DepartmentId(request.DepartmentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Department), request.DepartmentId);

        department.Activate();

        return DepartmentDto.FromDomain(department);
    }
}
