using Clovent.Identity.Branches;
using Clovent.Identity.Companies;

namespace Clovent.MasterData.Application.Tests.TestSupport;

public sealed class FakeBranchRepository : IBranchRepository
{
    private readonly Dictionary<BranchId, Branch> _branches = [];

    public void Add(Branch branch) => _branches[branch.Id] = branch;

    public Task<Branch?> GetByIdAsync(BranchId id, CancellationToken cancellationToken = default)
    {
        _branches.TryGetValue(id, out var branch);
        return Task.FromResult(branch);
    }

    public Task<IReadOnlyCollection<Branch>> GetByCompanyIdAsync(CompanyId companyId, CancellationToken cancellationToken = default)
    {
        var matches = _branches.Values.Where(b => b.CompanyId == companyId).ToList();
        return Task.FromResult<IReadOnlyCollection<Branch>>(matches);
    }

    public Task AddAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        _branches[branch.Id] = branch;
        return Task.CompletedTask;
    }
}
