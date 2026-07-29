using Clovent.Identity.Organizations;

namespace Clovent.MasterData.FiscalYears;

/// <summary>Persistence contract for <see cref="FiscalYear"/> aggregates.</summary>
public interface IFiscalYearRepository
{
    /// <summary>Retrieves a fiscal year by identity, or <see langword="null"/> if none exists.</summary>
    Task<FiscalYear?> GetByIdAsync(FiscalYearId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every fiscal year belonging to an organization.</summary>
    Task<IReadOnlyCollection<FiscalYear>> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created fiscal year.</summary>
    Task AddAsync(FiscalYear fiscalYear, CancellationToken cancellationToken = default);
}
