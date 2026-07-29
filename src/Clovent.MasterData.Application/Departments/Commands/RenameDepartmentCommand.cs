using Clovent.MasterData.Application.Departments.Dtos;
using Clovent.MasterData.Departments;
using Clovent.MasterData.Departments.ValueObjects;
using MediatR;

namespace Clovent.MasterData.Application.Departments.Commands;

/// <summary>Renames an existing department.</summary>
public sealed record RenameDepartmentCommand(Guid DepartmentId, string Name) : IRequest<DepartmentDto>;

/// <summary>Handles <see cref="RenameDepartmentCommand"/>.</summary>
public sealed class RenameDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    : IRequestHandler<RenameDepartmentCommand, DepartmentDto>
{
    /// <inheritdoc/>
    public async Task<DepartmentDto> Handle(RenameDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetByIdAsync(new DepartmentId(request.DepartmentId), cancellationToken)
            ?? throw new NotFoundException(nameof(Department), request.DepartmentId);

        department.Rename(DepartmentName.Create(request.Name));

        return DepartmentDto.FromDomain(department);
    }
}
