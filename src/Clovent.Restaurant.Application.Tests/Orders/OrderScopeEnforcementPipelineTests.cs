using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.MasterData.Shared;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Clovent.Platform.Execution;
using Clovent.Restaurant.Application.DependencyInjection;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
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
    private readonly FakeTableRepository _tableRepository;
    private readonly FakeWarehouseRepository _warehouseRepository;
    private readonly FakeBranchRepository _branchRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;

    public OrderScopeEnforcementPipelineTests()
    {
        var services = new ServiceCollection();
        _accessor = new ExecutionContextAccessor();
        services.AddSingleton(_accessor);

        _orderRepository = new FakeOrderRepository();
        _tableRepository = new FakeTableRepository();
        _warehouseRepository = new FakeWarehouseRepository();
        _branchRepository = new FakeBranchRepository();

        services.AddSingleton<IOrderRepository>(_orderRepository);
        services.AddSingleton<ITableRepository>(_tableRepository);
        services.AddSingleton<IWarehouseRepository>(_warehouseRepository);
        services.AddSingleton<IBranchRepository>(_branchRepository);
        services.AddSingleton<IOrderLineRepository>(new FakeOrderLineRepository());
        services.AddSingleton<IPaymentRepository>(new FakePaymentRepository());
        services.AddSingleton<IPaymentMethodRepository>(new FakePaymentMethodRepository());
        services.AddSingleton<Clovent.Restaurant.Customers.ICustomerRepository>(new FakeCustomerRepository());
        services.AddSingleton<Clovent.Restaurant.Customers.ICustomerLedgerEntryRepository>(new FakeCustomerLedgerEntryRepository());

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
    public async Task GetOrderByIdQuery_WithMatchingBranchScope_ExecutesSuccessfully()
    {
        var (branchA, warehouseA) = CreateBranchAndWarehouse(Guid.NewGuid(), "Branch A", "WH A");
        var order = Order.Create(OrderType.TakeAway, warehouseA.Id);
        _orderRepository.Add(order);

        var context = new PlatformExecutionContext
        {
            OrganizationId = Guid.NewGuid(),
            CompanyId = branchA.CompanyId.Value,
            BranchId = branchA.Id.Value
        };

        using (_accessor.BeginScope(context))
        {
            var result = await _mediator.Send(new GetOrderByIdQuery(order.Id.Value));
            Assert.NotNull(result);
            Assert.Equal(order.Id.Value, result.OrderId);
        }
    }

    [Fact]
    public async Task GetOrderByIdQuery_CallerInBranchA_SuppliesBranchBOrderId_ThrowsScopeMismatchException()
    {
        var (branchA, _) = CreateBranchAndWarehouse(Guid.NewGuid(), "Branch A", "WH A");
        var (branchB, warehouseB) = CreateBranchAndWarehouse(Guid.NewGuid(), "Branch B", "WH B");

        // Order created at Branch B's warehouse
        var orderB = Order.Create(OrderType.TakeAway, warehouseB.Id);
        _orderRepository.Add(orderB);

        // Caller is authorized for Branch A
        var contextBranchA = new PlatformExecutionContext
        {
            OrganizationId = Guid.NewGuid(),
            CompanyId = branchA.CompanyId.Value,
            BranchId = branchA.Id.Value
        };

        using (_accessor.BeginScope(contextBranchA))
        {
            // Branch A caller queries Branch B's order ID
            var ex = await Assert.ThrowsAsync<ScopeMismatchException>(
                () => _mediator.Send(new GetOrderByIdQuery(orderB.Id.Value)));

            Assert.Contains("Access denied", ex.Message);
            Assert.Contains(branchB.Id.Value.ToString(), ex.Message);
            Assert.Contains(branchA.Id.Value.ToString(), ex.Message);
        }
    }

    [Fact]
    public async Task VoidOrderCommand_CallerInBranchA_SuppliesBranchBOrderId_ThrowsScopeMismatchAndDoesNotMutateOrder()
    {
        var (branchA, _) = CreateBranchAndWarehouse(Guid.NewGuid(), "Branch A", "WH A");
        var (branchB, warehouseB) = CreateBranchAndWarehouse(Guid.NewGuid(), "Branch B", "WH B");

        var orderB = Order.Create(OrderType.TakeAway, warehouseB.Id);
        _orderRepository.Add(orderB);
        Assert.Equal(OrderStatus.Open, orderB.Status);

        var contextBranchA = new PlatformExecutionContext
        {
            OrganizationId = Guid.NewGuid(),
            CompanyId = branchA.CompanyId.Value,
            BranchId = branchA.Id.Value
        };

        using (_accessor.BeginScope(contextBranchA))
        {
            var ex = await Assert.ThrowsAsync<ScopeMismatchException>(
                () => _mediator.Send(new VoidOrderCommand(orderB.Id.Value, "Unauthorized void attempt")));

            Assert.Contains("Access denied", ex.Message);
            Assert.Contains(branchB.Id.Value.ToString(), ex.Message);
        }

        // Verify order record was NOT mutated
        Assert.Equal(OrderStatus.Open, orderB.Status);
    }

    [Fact]
    public async Task CancelOrderCommand_CallerInBranchA_SuppliesBranchBOrderId_ThrowsScopeMismatchAndDoesNotMutateOrderOrTable()
    {
        var (branchA, _) = CreateBranchAndWarehouse(Guid.NewGuid(), "Branch A", "WH A");
        var (branchB, warehouseB) = CreateBranchAndWarehouse(Guid.NewGuid(), "Branch B", "WH B");

        var table = Table.Create(DiningAreaId.New(), EntityCode.Create("T-01"), 4);
        table.Occupy();
        _tableRepository.Add(table);

        var orderB = Order.Create(OrderType.DineIn, warehouseB.Id, table.Id);
        _orderRepository.Add(orderB);
        Assert.Equal(OrderStatus.Open, orderB.Status);
        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);

        var contextBranchA = new PlatformExecutionContext
        {
            OrganizationId = Guid.NewGuid(),
            CompanyId = branchA.CompanyId.Value,
            BranchId = branchA.Id.Value
        };

        using (_accessor.BeginScope(contextBranchA))
        {
            var ex = await Assert.ThrowsAsync<ScopeMismatchException>(
                () => _mediator.Send(new CancelOrderCommand(orderB.Id.Value, "Unauthorized cancel attempt")));

            Assert.Contains("Access denied", ex.Message);
            Assert.Contains(branchB.Id.Value.ToString(), ex.Message);
        }

        // Verify order was NOT cancelled and table was NOT vacated
        Assert.Equal(OrderStatus.Open, orderB.Status);
        Assert.Equal(TableOccupancyStatus.Occupied, table.OccupancyStatus);
    }

    [Fact]
    public async Task GetEndOfDayReportQuery_OutOfCompanyWarehouseTarget_ThrowsScopeMismatchException()
    {
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        var (branchA, _) = CreateBranchAndWarehouse(companyA, "Branch A", "WH A");
        var (_, warehouseB) = CreateBranchAndWarehouse(companyB, "Branch B", "WH B");

        // Caller is authorized for Company A (company-level report query, no branch scope required)
        var contextCompanyA = new PlatformExecutionContext
        {
            OrganizationId = Guid.NewGuid(),
            CompanyId = companyA,
            BranchId = null
        };

        using (_accessor.BeginScope(contextCompanyA))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            // Caller requests report for Warehouse B belonging to Company B
            var ex = await Assert.ThrowsAsync<ScopeMismatchException>(
                () => _mediator.Send(new GetEndOfDayReportQuery(warehouseB.Id.Value, today, today)));

            Assert.Contains("Access denied", ex.Message);
            Assert.Contains(companyB.ToString(), ex.Message);
            Assert.Contains(companyA.ToString(), ex.Message);
        }
    }

    [Fact]
    public async Task GetEndOfDayReportQuery_MatchingCompanyWarehouseTarget_SucceedsWithoutRequiringBranchScope()
    {
        var companyA = Guid.NewGuid();
        var (branchA, warehouseA) = CreateBranchAndWarehouse(companyA, "Branch A", "WH A");

        var contextCompanyA = new PlatformExecutionContext
        {
            OrganizationId = Guid.NewGuid(),
            CompanyId = companyA,
            BranchId = null // BranchId is deliberately null
        };

        using (_accessor.BeginScope(contextCompanyA))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var report = await _mediator.Send(new GetEndOfDayReportQuery(warehouseA.Id.Value, today, today));

            Assert.NotNull(report);
            Assert.Equal(0, report.ReceiptCount);
        }
    }

    private (Branch branch, Warehouse warehouse) CreateBranchAndWarehouse(Guid companyId, string branchName, string warehouseName)
    {
        var branch = Branch.Create(new CompanyId(companyId), BranchName.Create(branchName));
        _branchRepository.Add(branch);

        var warehouse = Warehouse.Create(branch.Id, WarehouseName.Create(warehouseName), EntityCode.Create("W" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()));
        _warehouseRepository.Add(warehouse);

        return (branch, warehouse);
    }

    private sealed class FakeWarehouseRepository : IWarehouseRepository
    {
        private readonly Dictionary<WarehouseId, Warehouse> _warehouses = new();

        public void Add(Warehouse warehouse) => _warehouses[warehouse.Id] = warehouse;

        public Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken ct = default) =>
            Task.FromResult(_warehouses.GetValueOrDefault(id));

        public Task<IReadOnlyCollection<Warehouse>> GetByBranchIdAsync(BranchId branchId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<Warehouse>>(_warehouses.Values.Where(w => w.BranchId == branchId).ToList());

        public Task<IReadOnlyCollection<Warehouse>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<Warehouse>>(_warehouses.Values.ToList());

        public Task AddAsync(Warehouse warehouse, CancellationToken ct = default)
        {
            Add(warehouse);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBranchRepository : IBranchRepository
    {
        private readonly Dictionary<BranchId, Branch> _branches = new();

        public void Add(Branch branch) => _branches[branch.Id] = branch;

        public Task<Branch?> GetByIdAsync(BranchId id, CancellationToken ct = default) =>
            Task.FromResult(_branches.GetValueOrDefault(id));

        public Task<IReadOnlyCollection<Branch>> GetByCompanyIdAsync(CompanyId compId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyCollection<Branch>>(_branches.Values.Where(b => b.CompanyId == compId).ToList());

        public Task AddAsync(Branch branch, CancellationToken ct = default)
        {
            Add(branch);
            return Task.CompletedTask;
        }
    }
}
