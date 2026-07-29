using Clovent.Identity.Branches;
using Clovent.Identity.Companies;

namespace Clovent.Identity.Application.Tests.TestSupport;

internal sealed class FakeBranchRepository : IBranchRepository
{
    private readonly Dictionary<BranchId, Branch> _branches = [];

    public void Add(Branch branch) => _branches[branch.Id] = branch;

    public Task<Branch?> GetByIdAsync(BranchId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_branches.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Branch>> GetByCompanyIdAsync(CompanyId companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Branch>>([.. _branches.Values.Where(b => b.CompanyId == companyId)]);

    public Task AddAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        _branches[branch.Id] = branch;
        return Task.CompletedTask;
    }
}
