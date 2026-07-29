using Clovent.Domain;
using Clovent.Identity.Organizations;
using Clovent.MasterData.FiscalYears.Events;
using Clovent.MasterData.FiscalYears.ValueObjects;

namespace Clovent.MasterData.FiscalYears;

/// <summary>
/// A financial reporting period for an <see cref="Organization"/>. Which
/// fiscal year is "current" is not tracked here (no <c>IsCurrent</c> flag) -
/// it is derived from <see cref="Clovent.MasterData.Settings.BusinessSettings.DefaultFiscalYearId"/>,
/// a single source of truth rather than two places that could drift apart.
/// </summary>
public sealed class FiscalYear : AggregateRoot<FiscalYearId>
{
    /// <summary>The organization this fiscal year belongs to, fixed at creation.</summary>
    public OrganizationId OrganizationId { get; }

    /// <summary>The fiscal year's display label.</summary>
    public FiscalYearName Name { get; private set; }

    /// <summary>The first day of the fiscal year, inclusive.</summary>
    public DateOnly StartDate { get; }

    /// <summary>The last day of the fiscal year, inclusive.</summary>
    public DateOnly EndDate { get; }

    /// <summary>The fiscal year's current lifecycle state.</summary>
    public FiscalYearStatus Status { get; private set; }

    /// <summary>UTC instant this fiscal year was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private FiscalYear(
        FiscalYearId id,
        OrganizationId organizationId,
        FiscalYearName name,
        DateOnly startDate,
        DateOnly endDate,
        FiscalYearStatus status,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, open fiscal year for the given organization.</summary>
    /// <exception cref="MasterDataDomainException"><paramref name="endDate"/> is not after <paramref name="startDate"/>.</exception>
    public static FiscalYear Create(OrganizationId organizationId, FiscalYearName name, DateOnly startDate, DateOnly endDate)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (endDate <= startDate)
            throw MasterDataDomainException.FiscalYearEndBeforeStart(startDate, endDate);

        var now = DateTimeOffset.UtcNow;
        var fiscalYear = new FiscalYear(FiscalYearId.New(), organizationId, name, startDate, endDate, FiscalYearStatus.Open, now);
        fiscalYear.AddDomainEvent(new FiscalYearCreated(fiscalYear.Id, fiscalYear.OrganizationId, fiscalYear.Name, startDate, endDate, now));
        return fiscalYear;
    }

    /// <summary>Renames the fiscal year. A no-op (no event raised) if unchanged.</summary>
    public void Rename(FiscalYearName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new FiscalYearRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Closes the fiscal year. This is a one-way transition - there is no Reopen().</summary>
    /// <exception cref="MasterDataDomainException">The fiscal year is already closed.</exception>
    public void Close()
    {
        if (Status == FiscalYearStatus.Closed)
            throw MasterDataDomainException.FiscalYearAlreadyClosed(Id);

        Status = FiscalYearStatus.Closed;
        AddDomainEvent(new FiscalYearClosed(Id, DateTimeOffset.UtcNow));
    }
}
