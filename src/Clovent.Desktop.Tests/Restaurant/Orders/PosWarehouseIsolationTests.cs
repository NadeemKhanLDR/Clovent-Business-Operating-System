using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Dashboard;
using Clovent.Desktop.Forms.Restaurant.MenuItems;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Notifications;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Security;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Shell;
using Clovent.Desktop.Startup;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Branches.Dtos;
using Clovent.Identity.Application.Branches.Queries;
using Clovent.Identity.Application.Companies.Dtos;
using Clovent.Identity.Application.Companies.Queries;
using Clovent.Identity.Application.Organizations.Dtos;
using Clovent.Identity.Application.Organizations.Queries;
using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Queries;
using Clovent.Platform.Execution;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public sealed class PosWarehouseIsolationTests : IDisposable
{
    private readonly string _testSettingsPath;

    public PosWarehouseIsolationTests()
    {
        _testSettingsPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pos_settings_test_{Guid.NewGuid():N}.json");
        PosSettingsStore.SetTestingOverrides(_testSettingsPath);
    }

    public void Dispose()
    {
        PosSettingsStore.ResetTestingOverrides();
        if (System.IO.File.Exists(_testSettingsPath))
        {
            try { System.IO.File.Delete(_testSettingsPath); } catch { }
        }
    }

    [Fact]
    public void EntityPicker_WhenAutoSelectFalseAndNoInitial_LeavesSelectionNull()
    {
        using var picker = new EntityPicker("Location:");
        var wh1 = Guid.NewGuid();
        var wh2 = Guid.NewGuid();
        var items = new List<(Guid Id, string Display)>
        {
            (wh1, "Warehouse 1"),
            (wh2, "Warehouse 2")
        };

        picker.LoadItems(items, autoSelectFirst: false, initialSelectedId: null);

        Assert.Null(picker.SelectedId);
    }

    [Fact]
    public void EntityPicker_WhenInitialSelectedProvided_SelectsConfiguredTarget()
    {
        using var picker = new EntityPicker("Location:");
        var wh1 = Guid.NewGuid();
        var wh2 = Guid.NewGuid();
        var items = new List<(Guid Id, string Display)>
        {
            (wh1, "Warehouse 1"),
            (wh2, "Warehouse 2")
        };

        picker.LoadItems(items, autoSelectFirst: false, initialSelectedId: wh2);

        Assert.Equal(wh2, picker.SelectedId);
    }

    [Fact]
    public void EntityPicker_ClearSelection_ResetsSelectionToNull()
    {
        using var picker = new EntityPicker("Location:");
        var wh1 = Guid.NewGuid();
        var items = new List<(Guid Id, string Display)> { (wh1, "Warehouse 1") };

        picker.LoadItems(items, autoSelectFirst: true);
        Assert.Equal(wh1, picker.SelectedId);

        picker.ClearSelection();
        Assert.Null(picker.SelectedId);
    }

    [Fact]
    public async Task Pos_EnsureWarehouseSelected_DefaultsToAuthorizedConfigured_NeverIndex0OnMissing()
    {
        var authWh1 = Guid.NewGuid();
        var authWh2 = Guid.NewGuid();
        var foreignWh = Guid.NewGuid();

        var mediator = new MockWarehouseMediator
        {
            Warehouses =
            [
                new WarehouseDto(authWh1, Guid.NewGuid(), "Main Warehouse", "WH-01", "Active", DateTimeOffset.UtcNow),
                new WarehouseDto(authWh2, Guid.NewGuid(), "Secondary Warehouse", "WH-02", "Active", DateTimeOffset.UtcNow)
            ]
        };

        // Case 1: Configured warehouse is foreign/unauthorized -> must NOT pick index 0!
        PosSettingsStore.SaveWarehouseId(foreignWh);
        using var form1 = CreateMockPosForm(mediator);
        var ensureMethod = typeof(RestaurantPosForm).GetMethod("EnsureWarehouseSelectedAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var selectedForeign = await (Task<Guid?>)ensureMethod.Invoke(form1, null)!;
        Assert.Null(selectedForeign);

        // Case 2: Configured warehouse is authorized WH-02 -> defaults to WH-02
        PosSettingsStore.SaveWarehouseId(authWh2);
        using var form2 = CreateMockPosForm(mediator);

        var selectedAuth = await (Task<Guid?>)ensureMethod.Invoke(form2, null)!;
        Assert.Equal(authWh2, selectedAuth);
    }

    [Fact]
    public async Task DashboardView_BindsLabelsToActiveContext_DoesNotFallBackToFirstUnrelated()
    {
        var compA = Guid.NewGuid();
        var branchA = Guid.NewGuid();
        var compB = Guid.NewGuid();
        var branchB = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var mediator = new MockDashboardMediator
        {
            Organization = new OrganizationDto(orgId, "Acme Corp", "TAX-ORG", "Active", DateTimeOffset.UtcNow, [compA, compB]),
            Companies = new Dictionary<Guid, CompanyDto>
            {
                [compA] = new CompanyDto(compA, orgId, "Company Alpha", "TAX-A", "Active", DateTimeOffset.UtcNow, [branchA]),
                [compB] = new CompanyDto(compB, orgId, "Company Beta", "TAX-B", "Active", DateTimeOffset.UtcNow, [branchB])
            },
            Branches = new Dictionary<Guid, BranchDto>
            {
                [branchA] = new BranchDto(branchA, compA, "Branch Alpha", null, null, null, null, null, "Active", DateTimeOffset.UtcNow),
                [branchB] = new BranchDto(branchB, compB, "Branch Beta", null, null, null, null, null, "Active", DateTimeOffset.UtcNow)
            }
        };

        var scopeProvider = new DesktopInteractiveScopeProvider();
        // Set active context specifically to Company Beta and Branch Beta
        scopeProvider.SetCurrentContext(new PlatformExecutionContext
        {
            UserId = Guid.NewGuid(),
            OrganizationId = orgId,
            CompanyId = compB,
            BranchId = branchB
        });

        var session = new CurrentSession();
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "Test Cashier");

        var services = new ServiceCollection();
        services.AddSingleton<IMediator>(mediator);
        services.AddSingleton<IInteractiveScopeProvider>(scopeProvider);
        services.AddSingleton<IExecutionContextAccessor>(new ExecutionContextAccessor());
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        using var view = new DashboardView(
            scopeFactory,
            session,
            new NotificationService(),
            new RecentItemsService());

        typeof(DashboardView).GetField("_scope", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(view, sp.CreateScope());
        typeof(DashboardView).GetField("_mediator", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(view, mediator);

        var method = typeof(DashboardView).GetMethod("LoadBusinessContextAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(view, null)!;

        var lblComp = (DevExpress.XtraEditors.LabelControl)typeof(DashboardView).GetField("lblCompanyValue", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(view)!;
        var lblBranch = (DevExpress.XtraEditors.LabelControl)typeof(DashboardView).GetField("lblBranchValue", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(view)!;

        // Invariant: Company Beta and Branch Beta must be bound from verified active scope, NOT Company Alpha / Branch Alpha!
        Assert.Equal("Company Beta", lblComp.Text);
        Assert.Equal("Branch Beta", lblBranch.Text);
    }

    private static RestaurantPosForm CreateMockPosForm(IMediator mediator)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMediator>(mediator);
        var authMock = new MockFeaturePolicy();
        services.AddSingleton<IFeatureAuthorizationPolicy>(authMock);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var session = new CurrentSession();
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "Test Cashier");

        return new RestaurantPosForm(
            scopeFactory,
            session,
            new MockChangeNotifier(),
            new MockManagerAuth(),
            new MockSplash());
    }

    private sealed class MockWarehouseMediator : IMediator
    {
        public List<WarehouseDto> Warehouses { get; set; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ListAllWarehousesQuery)
            {
                return Task.FromResult((TResponse)(object)(IReadOnlyCollection<WarehouseDto>)Warehouses);
            }

            var type = typeof(TResponse);
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(IReadOnlyList<>) || genericDef == typeof(IReadOnlyCollection<>) || genericDef == typeof(IList<>))
                {
                    var itemType = type.GetGenericArguments()[0];
                    var listType = typeof(List<>).MakeGenericType(itemType);
                    return Task.FromResult((TResponse)Activator.CreateInstance(listType)!);
                }
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockDashboardMediator : IMediator
    {
        public OrganizationDto? Organization { get; set; }
        public Dictionary<Guid, CompanyDto> Companies { get; set; } = [];
        public Dictionary<Guid, BranchDto> Branches { get; set; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetOrganizationByIdQuery)
            {
                return Task.FromResult((TResponse)(object)Organization!);
            }
            if (request is GetCompanyByIdQuery gc && Companies.TryGetValue(gc.CompanyId, out var comp))
            {
                return Task.FromResult((TResponse)(object)comp);
            }
            if (request is GetBranchByIdQuery gb && Branches.TryGetValue(gb.BranchId, out var br))
            {
                return Task.FromResult((TResponse)(object)br);
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class MockFeaturePolicy : IFeatureAuthorizationPolicy
    {
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IReadOnlySet<string>> GetAllowedFeaturesAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());
    }

    private sealed class MockChangeNotifier : IMenuItemsChangeNotifier
    {
        public event EventHandler? Changed;
        public void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class MockManagerAuth : IManagerAuthorizationService
    {
        public Task<ManagerAuthorizationResult> AuthorizeAsync(string userName, string password, string featureCode, CancellationToken cancellationToken = default)
            => Task.FromResult(new ManagerAuthorizationResult(true, Guid.NewGuid(), "Admin", null));
    }

    private sealed class MockSplash : ISplashScreenService
    {
        public void Show(string caption, string description) { }
        public void SetDescription(string description) { }
        public void Close() { }
    }
}
