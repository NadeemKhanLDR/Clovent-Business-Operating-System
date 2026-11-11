using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

public sealed class SqlTestTargetGuardTests
{
    [Fact]
    public void ProductionPersistence_RegistersScopedDatabaseCoordination()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = SqlTestDatabaseGuard.ConnectionString($"CBOS_Recovery_{Guid.NewGuid():N}")
        }).Build();
        Clovent.Restaurant.Infrastructure.DependencyInjection.PersistenceServiceCollectionExtensions.AddPersistence(services, config);
        var registration = Assert.Single(services, s => s.ServiceType == typeof(Clovent.Restaurant.Application.Concurrency.ITransactionLockProvider));
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
        Assert.Equal(typeof(Clovent.Restaurant.Infrastructure.Persistence.SqlTransactionLockProvider), registration.ImplementationType);
    }
    [Theory]
    [InlineData("Server=.;Database=CBOS_Recovery_00000000000000000000000000000000;Integrated Security=true")]
    [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=Clovent_BusinessOperatingSystem;Integrated Security=true")]
    [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=CBOS_Recovery_00000000000000000000000000000000;Integrated Security=false")]
    public void UnsafeTarget_IsRejectedBeforeAnyConnectionIsOpened(string connection)
    {
        Assert.ThrowsAny<Exception>(() => SqlTestDatabaseGuard.Validate(connection));
    }
}