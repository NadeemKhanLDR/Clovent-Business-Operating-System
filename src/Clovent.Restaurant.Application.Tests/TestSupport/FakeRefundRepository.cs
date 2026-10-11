using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Refunds;
using Clovent.Restaurant.Shifts;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

public sealed class FakeRefundRepository : IRefundRepository
{
    private readonly List<Refund> _refunds = [];

    public Task<Refund?> GetByIdAsync(RefundId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_refunds.FirstOrDefault(r => r.Id == id));

    public Task<Refund?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(_refunds.FirstOrDefault(r => string.Equals(r.IdempotencyKey, idempotencyKey, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Refund>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Refund>>(_refunds.Where(r => r.OrderId == orderId).ToList());

    public Task<IReadOnlyList<Refund>> GetByShiftIdAsync(ShiftId shiftId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Refund>>(_refunds.Where(r => r.ShiftId == shiftId).ToList());

    public Task<IReadOnlyList<Refund>> GetByDateRangeAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Refund>>(
            _refunds.Where(r => r.RefundedAtUtc >= fromUtc && r.RefundedAtUtc <= toUtc).ToList());

    public Task<IReadOnlyList<Refund>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Refund>>(_refunds.ToList());

    public Task AddAsync(Refund refund, CancellationToken cancellationToken = default)
    {
        _refunds.Add(refund);
        return Task.CompletedTask;
    }
}
