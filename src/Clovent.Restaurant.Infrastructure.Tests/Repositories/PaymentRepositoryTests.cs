using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

public class PaymentRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var orderId = OrderId.New();
        var paymentMethodId = PaymentMethodId.New();
        var payment = Payment.Create(orderId, paymentMethodId, 25m);

        await using (var writeContext = CreateContext())
        {
            var repository = new PaymentRepository(writeContext);
            await repository.AddAsync(payment);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new PaymentRepository(readContext).GetByIdAsync(payment.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(orderId, reloaded!.OrderId);
        Assert.Equal(paymentMethodId, reloaded.PaymentMethodId);
        Assert.Equal(25m, reloaded.Amount);
        Assert.False(reloaded.IsVoided);
    }

    [Fact]
    public async Task GetByOrderIdAsync_FiltersToOwningOrder()
    {
        var orderId = OrderId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new PaymentRepository(writeContext);
            await repository.AddAsync(Payment.Create(orderId, PaymentMethodId.New(), 10m));
            await repository.AddAsync(Payment.Create(OrderId.New(), PaymentMethodId.New(), 20m));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new PaymentRepository(readContext).GetByOrderIdAsync(orderId);

        Assert.Single(found);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new PaymentRepository(context).GetByIdAsync(PaymentId.New());

        Assert.Null(result);
    }
}
