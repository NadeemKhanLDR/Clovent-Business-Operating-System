using Clovent.Identity.Companies;
using Clovent.Identity.Organizations;

namespace Clovent.Identity.Application.Tests.TestSupport;

internal sealed class FakeCompanyRepository : ICompanyRepository
{
    private readonly Dictionary<CompanyId, Company> _companies = [];

    public void Add(Company company) => _companies[company.Id] = company;

    public Task<Company?> GetByIdAsync(CompanyId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_companies.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Company>> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Company>>([.. _companies.Values.Where(c => c.OrganizationId == organizationId)]);

    public Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        _companies[company.Id] = company;
        return Task.CompletedTask;
    }
}
