using Clovent.MasterData.Application.Departments.Dtos;
using Clovent.MasterData.Departments;
using MediatR;

namespace Clovent.MasterData.Application.Departments.Commands;

/// <summary>Deactivates a department.</summary>
public sealed record DeactivateDepartmentCommand(Guid DepartmentId) : IRequest<DepartmentDto>;

/// <summary>Handles <see cref="DeactivateDepartmentCommand"/>.</summary>
public sealed class DeactivateDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    : IRequestHandler<DeactivateDepartmentCommand, DepartmentDto>
{
    /// <inheritdoc/>
    public async Task<DepartmentDto> Handle(DeactivateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetByIdAsync(new DepartmentId(request.DepartmentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Department), request.DepartmentId);

        department.Deactivate();

        return DepartmentDto.FromDomain(department);
    }
}
