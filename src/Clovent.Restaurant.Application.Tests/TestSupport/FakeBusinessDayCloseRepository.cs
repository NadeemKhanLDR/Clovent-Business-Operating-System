using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Branches;
using Clovent.Restaurant.DayClose;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakeBusinessDayCloseRepository : IBusinessDayCloseRepository
{
    private readonly Dictionary<BusinessDayCloseId, BusinessDayClose> _closes = [];

    public Task<BusinessDayClose?> GetByIdAsync(BusinessDayCloseId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_closes.GetValueOrDefault(id));

    public Task<BusinessDayClose?> GetByBranchAndDateAsync(BranchId branchId, DateOnly businessDate, CancellationToken cancellationToken = default) =>
        Task.FromResult(_closes.Values.FirstOrDefault(c => c.BranchId == branchId && c.BusinessDate == businessDate));

    public Task<IReadOnlyList<BusinessDayClose>> ListByBranchAndDateRangeAsync(
        BranchId branchId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var list = _closes.Values
            .Where(c => c.BranchId == branchId && c.BusinessDate >= fromDate && c.BusinessDate <= toDate)
            .OrderByDescending(c => c.BusinessDate)
            .ToList();
        return Task.FromResult<IReadOnlyList<BusinessDayClose>>(list);
    }

    public Task AddAsync(BusinessDayClose businessDayClose, CancellationToken cancellationToken = default)
    {
        _closes[businessDayClose.Id] = businessDayClose;
        return Task.CompletedTask;
    }
}
