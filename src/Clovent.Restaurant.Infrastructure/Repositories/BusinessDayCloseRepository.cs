using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Restaurant.DayClose;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IBusinessDayCloseRepository"/>.</summary>
public sealed class BusinessDayCloseRepository(RestaurantDbContext dbContext) : IBusinessDayCloseRepository
{
    /// <inheritdoc/>
    public async Task<BusinessDayClose?> GetByIdAsync(BusinessDayCloseId id, CancellationToken cancellationToken = default) =>
        await dbContext.BusinessDayCloses.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<BusinessDayClose?> GetByBranchAndDateAsync(BranchId branchId, DateOnly businessDate, CancellationToken cancellationToken = default) =>
        await dbContext.BusinessDayCloses.FirstOrDefaultAsync(c => c.BranchId == branchId && c.BusinessDate == businessDate, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BusinessDayClose>> ListByBranchAndDateRangeAsync(
        BranchId branchId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default) =>
        await dbContext.BusinessDayCloses
            .Where(c => c.BranchId == branchId && c.BusinessDate >= fromDate && c.BusinessDate <= toDate)
            .OrderByDescending(c => c.BusinessDate)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(BusinessDayClose businessDayClose, CancellationToken cancellationToken = default) =>
        await dbContext.BusinessDayCloses.AddAsync(businessDayClose, cancellationToken);
}
