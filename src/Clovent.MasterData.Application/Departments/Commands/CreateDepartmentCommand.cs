using Clovent.Identity.Branches;
using Clovent.MasterData.Application.Departments.Dtos;
using Clovent.MasterData.Departments;
using Clovent.MasterData.Departments.ValueObjects;
using MediatR;

namespace Clovent.MasterData.Application.Departments.Commands;

/// <summary>Creates a new department under an existing branch.</summary>
public sealed record CreateDepartmentCommand(Guid BranchId, string Name) : IRequest<DepartmentDto>;

/// <summary>Handles <see cref="CreateDepartmentCommand"/>.</summary>
public sealed class CreateDepartmentCommandHandler(IDepartmentRepository departmentRepository)
    : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    /// <inheritdoc/>
    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = Department.Create(new BranchId(request.BranchId), DepartmentName.Create(request.Name));

        await departmentRepository.AddAsync(department, cancellationToken);

        return DepartmentDto.FromDomain(department);
    }
}
