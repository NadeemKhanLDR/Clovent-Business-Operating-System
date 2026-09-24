using Clovent.MasterData.Shared.ValueObjects;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Tables.Queries;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Integration;

/// <summary>
/// TABLE-SWITCH-10/11: the dine-in table switch must survive persistence and
/// a simulated restart. Real EF Core repositories against SQLite, wired
/// through the real MediatR pipeline with <c>UnitOfWorkBehavior</c>, so only a
/// handler that completes commits anything - proving a rejected switch writes
/// nothing and a successful switch writes order, both tables and both
/// occupancy flags as one consistent unit.
/// </summary>
public sealed class TableSwitchingIntegrationTests : SqliteTestBase
{
    private readonly RestaurantDbContext _db;
    private readonly ServiceCollection _services = new();

    public TableSwitchingIntegrationTests()
    {
        // One shared DbContext for the handler-side registrations - mirrors how
        // OrderLifecycleIntegrationTests wires the module - while restart-side
        // reads use SqliteTestBase.CreateContext() for a brand-new instance
        // over the same connection.
        _db = CreateContext();

        _services.AddScoped<ITableRepository>(_ => new TableRepository(_db));
        _services.AddScoped<IOrderRepository>(_ => new OrderRepository(_db));
        _services.AddScoped<Clovent.Restaurant.Orders.IOrderNumberSequenceRepository>(_ => new OrderNumberSequenceRepository(_db));
        _services.AddScoped<Clovent.Restaurant.Application.IUnitOfWork>(_ => new UnitOfWork(_db));
        _services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        _services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(Clovent.Restaurant.Application.DependencyInjection.ApplicationServiceCollectionExtensions).Assembly));
    }

    private IMediator BuildMediator() => _services.BuildServiceProvider().GetRequiredService<IMediator>();

    private async Task<(Guid T01, Guid T02)> SeedTablesAsync()
    {
        var t01 = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        var t02 = Table.Create(DiningAreaId.New(), EntityCode.Create("T-02"), 4);
        await _db.Tables.AddRangeAsync(t01, t02);
        await _db.SaveChangesAsync();
        return (t01.Id.Value, t02.Id.Value);
    }

    /// <summary>TABLE-SWITCH-10 + 11: the switch persists - a fresh DbContext (a simulated restart) sees the order on the new table with occupancy to match.</summary>
    [Fact]
    public async Task Transfer_PersistsAcrossRestart()
    {
        var mediator = BuildMediator();
        var (t01, t02) = await SeedTablesAsync();

        var order = await mediator.Send(new CreateOrderCommand(OrderType.DineIn, Guid.NewGuid(), t01));
        var transferred = await mediator.Send(new TransferOrderTableCommand(order.OrderId, t02));

        Assert.Equal(t02, transferred.TableId);

        // Simulated restart: everything below is read through a brand-new
        // DbContext instance, not the tracked one the handler used.
        await using var restart = CreateContext();
        var persisted = await restart.Orders.SingleAsync(o => o.Id == new OrderId(order.OrderId));
        Assert.Equal(new TableId(t02), persisted.TableId);

        var tables = await BuildMediator().Send(new ListAllTablesQuery());
        Assert.Equal("Available", tables.Single(t => t.TableId == t01).OccupancyStatus);
        Assert.Equal("Occupied", tables.Single(t => t.TableId == t02).OccupancyStatus);
    }

    /// <summary>TABLE-SWITCH-08 (persistence half): a rejected switch commits nothing - a fresh DbContext still sees the original state.</summary>
    [Fact]
    public async Task RejectedTransfer_PersistsNothing()
    {
        var mediator = BuildMediator();
        var (t01, t02) = await SeedTablesAsync();

        var orderA = await mediator.Send(new CreateOrderCommand(OrderType.DineIn, Guid.NewGuid(), t01));
        var orderB = await mediator.Send(new CreateOrderCommand(OrderType.DineIn, Guid.NewGuid(), t02));

        await Assert.ThrowsAsync<RestaurantDomainException>(() =>
            mediator.Send(new TransferOrderTableCommand(orderA.OrderId, t02)));

        await using var restart = CreateContext();
        var persistedA = await restart.Orders.SingleAsync(o => o.Id == new OrderId(orderA.OrderId));
        var persistedB = await restart.Orders.SingleAsync(o => o.Id == new OrderId(orderB.OrderId));
        Assert.Equal(new TableId(t01), persistedA.TableId);
        Assert.Equal(new TableId(t02), persistedB.TableId);

        var tables = await BuildMediator().Send(new ListAllTablesQuery());
        Assert.Equal("Occupied", tables.Single(t => t.TableId == t01).OccupancyStatus);
        Assert.Equal("Occupied", tables.Single(t => t.TableId == t02).OccupancyStatus);
    }
}
