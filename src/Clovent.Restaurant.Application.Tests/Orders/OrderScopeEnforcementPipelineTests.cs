using Clovent.Platform.Execution;
using Clovent.Restaurant.Application.DependencyInjection;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Orders;

public sealed class OrderScopeEnforcementPipelineTests
{
    private readonly IExecutionContextAccessor _accessor;
    private readonly FakeOrderRepository _orderRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public OrderScopeEnforcementPipelineTests()
    {
        var services = new ServiceCollection();
        _accessor = new ExecutionContextAccessor();
        services.AddSingleton(_accessor);

        _orderRepository = new FakeOrderRepository();
        services.AddSingleton<IOrderRepository>(_orderRepository);
        services.AddSingleton<ITableRepository>(new FakeTableRepository());

        var config = new ConfigurationBuilder().Build();
        services.AddApplication(config);

        _serviceProvider = services.BuildServiceProvider();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task GetOrderByIdQuery_WithoutScope_ThrowsScopeUnresolvedException()
    {
        var query = new GetOrderByIdQuery(Guid.NewGuid());

        await Assert.ThrowsAsync<ScopeUnresolvedException>(() => _mediator.Send(query));
    }

    [Fact]
    public async Task GetOrderByIdQuery_WithValidBranchScope_ExecutesHandler()
    {
        var order = Order.Create(OrderType.TakeAway, new Clovent.MasterData.Warehouses.WarehouseId(Guid.NewGuid()));
        _orderRepository.Add(order);

        var context = new PlatformExecutionContext
        {
            OrganizationId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            BranchId = Guid.NewGuid()
        };

        using (_accessor.BeginScope(context))
        {
            var result = await _mediator.Send(new GetOrderByIdQuery(order.Id.Value));
            Assert.NotNull(result);
            Assert.Equal(order.Id.Value, result.OrderId);
        }
    }

    [Fact]
    public async Task VoidOrderCommand_WithoutScope_ThrowsScopeUnresolvedException()
    {
        var command = new VoidOrderCommand(Guid.NewGuid(), "Customer cancelled");

        await Assert.ThrowsAsync<ScopeUnresolvedException>(() => _mediator.Send(command));
    }

    [Fact]
    public async Task CancelOrderCommand_WithoutScope_ThrowsScopeUnresolvedException()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), "Customer cancelled");

        await Assert.ThrowsAsync<ScopeUnresolvedException>(() => _mediator.Send(command));
    }

    [Fact]
    public async Task GetEndOfDayReportQuery_WithoutCompanyScope_ThrowsScopeUnresolvedException()
    {
        // Only organization scope is present; company scope is missing
        var context = new PlatformExecutionContext
        {
            OrganizationId = Guid.NewGuid(),
            CompanyId = null,
            BranchId = null
        };

        using (_accessor.BeginScope(context))
        {
            var query = new GetEndOfDayReportQuery(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));
            await Assert.ThrowsAsync<ScopeUnresolvedException>(() => _mediator.Send(query));
        }
    }
}
