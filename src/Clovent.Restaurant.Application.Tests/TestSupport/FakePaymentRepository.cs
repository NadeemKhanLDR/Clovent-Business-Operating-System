using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakePaymentRepository : IPaymentRepository
{
    private readonly Dictionary<PaymentId, Payment> _payments = [];

    public void Add(Payment payment) => _payments[payment.Id] = payment;

    public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_payments.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Payment>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Payment>>([.. _payments.Values.Where(p => p.OrderId == orderId)]);

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _payments[payment.Id] = payment;
        return Task.CompletedTask;
    }
}
