using Clovent.Restaurant.PaymentMethods;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

internal sealed class FakePaymentMethodRepository : IPaymentMethodRepository
{
    private readonly Dictionary<PaymentMethodId, PaymentMethod> _methods = [];

    public void Add(PaymentMethod method) => _methods[method.Id] = method;

    public Task<PaymentMethod?> GetByIdAsync(PaymentMethodId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_methods.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<PaymentMethod>>([.. _methods.Values]);

    public Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default)
    {
        _methods[paymentMethod.Id] = paymentMethod;
        return Task.CompletedTask;
    }
}
