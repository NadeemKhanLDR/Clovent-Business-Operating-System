using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;

namespace Clovent.Restaurant.DayClose;

/// <summary>Repository contract for persisting and querying <see cref="BusinessDayClose"/> aggregates.</summary>
public interface IBusinessDayCloseRepository
{
    /// <summary>Gets a business day close by its unique ID.</summary>
    Task<BusinessDayClose?> GetByIdAsync(BusinessDayCloseId id, CancellationToken cancellationToken = default);

    /// <summary>Gets a business day close by branch and business operating date, or null if not yet closed.</summary>
    Task<BusinessDayClose?> GetByBranchAndDateAsync(BranchId branchId, DateOnly businessDate, CancellationToken cancellationToken = default);

    /// <summary>Lists historical business day closes for a branch within a date range.</summary>
    Task<IReadOnlyList<BusinessDayClose>> ListByBranchAndDateRangeAsync(
        BranchId branchId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    /// <summary>Adds a new business day close aggregate to persistence.</summary>
    Task AddAsync(BusinessDayClose businessDayClose, CancellationToken cancellationToken = default);
}
