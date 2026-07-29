using Clovent.Identity.Organizations;
using Clovent.MasterData.FiscalYears;

namespace Clovent.MasterData.Application.Tests.TestSupport;

internal sealed class FakeFiscalYearRepository : IFiscalYearRepository
{
    private readonly Dictionary<FiscalYearId, FiscalYear> _fiscalYears = [];

    public void Add(FiscalYear fiscalYear) => _fiscalYears[fiscalYear.Id] = fiscalYear;

    public Task<FiscalYear?> GetByIdAsync(FiscalYearId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_fiscalYears.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<FiscalYear>> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<FiscalYear>>([.. _fiscalYears.Values.Where(f => f.OrganizationId == organizationId)]);

    public Task AddAsync(FiscalYear fiscalYear, CancellationToken cancellationToken = default)
    {
        _fiscalYears[fiscalYear.Id] = fiscalYear;
        return Task.CompletedTask;
    }
}
