using Clovent.Identity.Branches;
using Clovent.MasterData.Departments;

namespace Clovent.MasterData.Application.Tests.TestSupport;

internal sealed class FakeDepartmentRepository : IDepartmentRepository
{
    private readonly Dictionary<DepartmentId, Department> _departments = [];

    public void Add(Department department) => _departments[department.Id] = department;

    public Task<Department?> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_departments.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Department>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Department>>([.. _departments.Values.Where(d => d.BranchId == branchId)]);

    public Task AddAsync(Department department, CancellationToken cancellationToken = default)
    {
        _departments[department.Id] = department;
        return Task.CompletedTask;
    }
}
