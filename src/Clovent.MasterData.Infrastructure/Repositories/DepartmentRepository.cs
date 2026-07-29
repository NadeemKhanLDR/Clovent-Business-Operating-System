using Clovent.Identity.Branches;
using Clovent.MasterData.Departments;
using Clovent.MasterData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IDepartmentRepository"/>.</summary>
public sealed class DepartmentRepository(MasterDataDbContext dbContext) : IDepartmentRepository
{
    /// <inheritdoc/>
    public Task<Department?> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken = default) =>
        dbContext.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Department>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
        await dbContext.Departments.Where(d => d.BranchId == branchId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Department department, CancellationToken cancellationToken = default) =>
        await dbContext.Departments.AddAsync(department, cancellationToken);
}
