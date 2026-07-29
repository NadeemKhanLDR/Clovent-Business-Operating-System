using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

public class PaymentMethodRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var method = PaymentMethod.Create(PaymentMethodName.Create("Cash"));

        await using (var writeContext = CreateContext())
        {
            var repository = new PaymentMethodRepository(writeContext);
            await repository.AddAsync(method);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new PaymentMethodRepository(readContext).GetByIdAsync(method.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(method.Name, reloaded!.Name);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryMethod()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new PaymentMethodRepository(writeContext);
            await repository.AddAsync(PaymentMethod.Create(PaymentMethodName.Create("Cash")));
            await repository.AddAsync(PaymentMethod.Create(PaymentMethodName.Create("Credit Card")));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new PaymentMethodRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new PaymentMethodRepository(context).GetByIdAsync(PaymentMethodId.New());

        Assert.Null(result);
    }
}
