using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Application.Customers.Queries;
using DevExpress.XtraGrid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Customers;

/// <summary>
/// Covers defects D4 (the grid stayed empty until Refresh was clicked) and D5
/// (Refresh re-filtered a cached list instead of re-reading).
/// </summary>
public class CustomersViewLoadAndRefreshTests
{
    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId { get; private set; }
        public Guid? SessionId { get; private set; }
        public string? DisplayName { get; private set; }
        public bool IsAuthenticated => UserId.HasValue;

        public void SignIn(Guid userId, Guid sessionId, string displayName)
        {
            UserId = userId;
            SessionId = sessionId;
            DisplayName = displayName;
        }

        public void SignOut()
        {
            UserId = null;
            SessionId = null;
            DisplayName = null;
        }

#pragma warning disable CS0067 // Event is never used in fake
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class AllowAllFeaturePolicy : IFeatureAuthorizationPolicy
    {
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    /// <summary>Answers each <see cref="ListCustomersQuery"/> from whatever <see cref="Customers"/> holds at that moment, and counts the calls.</summary>
    private sealed class CountingMediator : IMediator
    {
        public List<CustomerDto> Customers { get; set; } = [];
        public int ListCustomersCallCount { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ListCustomersQuery)
            {
                ListCustomersCallCount++;
                return Task.FromResult((TResponse)(object)(IReadOnlyList<CustomerDto>)[.. Customers]);
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            Task.FromResult<object?>(null);

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
            Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];

        public void Register<T>(T service) where T : class => _services[typeof(T)] = service;

        public object? GetService(Type serviceType) =>
            _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    private sealed class FakeServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public void Dispose()
        {
        }
    }

    private sealed class FakeServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FakeServiceScope(serviceProvider);
    }

    private static CustomerDto Customer(string code, string name, decimal outstanding) => new(
        CustomerId: Guid.NewGuid(),
        Code: code,
        Name: name,
        MobileNumber: "03001234567",
        Address: "Main Street",
        Email: null,
        OpeningBalance: 0m,
        CreditLimit: 500m,
        OutstandingBalance: outstanding,
        IsActive: true,
        Notes: null,
        CreatedAtUtc: DateTimeOffset.UtcNow,
        UpdatedAtUtc: DateTimeOffset.UtcNow);

    private static (CustomersView View, CountingMediator Mediator) CreateView()
    {
        var session = new FakeCurrentSession();
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "QA User");

        var mediator = new CountingMediator();

        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new AllowAllFeaturePolicy());
        provider.Register<ILogger<CustomersView>>(NullLogger<CustomersView>.Instance);

        return (new CustomersView(new FakeServiceScopeFactory(provider), session), mediator);
    }

    private static void RaiseLoad(CustomersView view)
    {
        var handler = typeof(CustomersView).GetMethod(
            "CustomersView_Load",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        handler.Invoke(view, [null, EventArgs.Empty]);
    }

    private static List<object> GridRows(CustomersView view)
    {
        var gridControl = (GridControl)typeof(CustomersView)
            .GetField("_gridControl", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(view)!;

        return gridControl.DataSource is System.Collections.IEnumerable rows
            ? [.. rows.Cast<object>()]
            : [];
    }

    /// <summary>
    /// D4's actual root cause: the handler existed but nothing subscribed it,
    /// so opening the screen never triggered a load. Asserting the subscription
    /// rather than just calling the handler is the point - calling it directly
    /// would have passed before the fix too.
    /// </summary>
    [Fact]
    public void CreatingTheControl_RaisesLoadAndFetchesCustomers()
    {
        var (view, mediator) = CreateView();
        using (view)
        {
            mediator.Customers = [Customer("C001", "John Smith", 272.50m)];

            // Deliberately goes through the real Load event rather than
            // invoking the handler by reflection: the handler was never the
            // problem, the missing subscription was, and only creating the
            // control exercises that.
            view.CreateControl();

            Assert.Equal(1, mediator.ListCustomersCallCount);
            Assert.Single(GridRows(view));
        }
    }

    /// <summary>D4: opening the screen populates the grid with no Refresh click.</summary>
    [Fact]
    public void Load_PopulatesTheGridWithoutAManualRefresh()
    {
        var (view, mediator) = CreateView();
        using (view)
        {
            mediator.Customers = [Customer("C001", "John Smith", 272.50m), Customer("C002", "Jane Doe", 0m)];

            RaiseLoad(view);

            Assert.Equal(1, mediator.ListCustomersCallCount);
            Assert.Equal(2, GridRows(view).Count);
        }
    }

    /// <summary>
    /// D5: the screen was re-filtering a list captured when it was constructed,
    /// so a balance changed elsewhere never appeared. Refresh must re-ask.
    /// </summary>
    [Fact]
    public void Refresh_ReQueriesAndShowsValuesChangedSinceTheLastLoad()
    {
        var (view, mediator) = CreateView();
        using (view)
        {
            mediator.Customers = [Customer("QACUST01", "QA-CUSTOMER-01", 317.50m)];
            RaiseLoad(view);

            Assert.Equal(317.50m, OutstandingOf(GridRows(view).Single()));

            // The balance moves underneath the open screen.
            mediator.Customers = [Customer("QACUST01", "QA-CUSTOMER-01", 999.99m)];

            InvokeRefresh(view);

            Assert.Equal(2, mediator.ListCustomersCallCount);
            Assert.Equal(999.99m, OutstandingOf(GridRows(view).Single()));
        }
    }

    /// <summary>The summary footer is recomputed from the refreshed result, not the one the screen opened with.</summary>
    [Fact]
    public void Refresh_RecalculatesTheSummaryMetrics()
    {
        var (view, mediator) = CreateView();
        using (view)
        {
            mediator.Customers = [Customer("QACUST01", "QA-CUSTOMER-01", 317.50m)];
            RaiseLoad(view);

            mediator.Customers = [Customer("QACUST01", "QA-CUSTOMER-01", 999.99m)];
            InvokeRefresh(view);

            var label = (DevExpress.XtraEditors.LabelControl)typeof(CustomersView)
                .GetField("_lblTotalOutstanding", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view)!;

            Assert.Contains("999.99", label.Text);
        }
    }

    /// <summary>The current search text survives a refresh - refreshing must not silently widen what the operator is looking at.</summary>
    [Fact]
    public void Refresh_KeepsTheActiveSearchFilterApplied()
    {
        var (view, mediator) = CreateView();
        using (view)
        {
            mediator.Customers = [Customer("C001", "John Smith", 10m), Customer("C002", "Jane Doe", 20m)];
            RaiseLoad(view);

            var search = (DevExpress.XtraEditors.TextEdit)typeof(CustomersView)
                .GetField("_txtSearch", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view)!;
            search.Text = "Jane";

            InvokeRefresh(view);

            var row = Assert.Single(GridRows(view));
            Assert.Equal("Jane Doe", NameOf(row));
        }
    }

    /// <summary>
    /// D22: this screen's scope holds one RestaurantDbContext and one
    /// IdentityDbContext, and EF Core allows only one operation in flight per
    /// context. Both services must therefore be the gated decorators, not the
    /// raw ones resolved from the scope.
    /// </summary>
    [Fact]
    public void ScopeServices_AreRoutedThroughTheScreenOperationGate()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var mediator = typeof(CustomersView)
                .GetField("_mediator", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view);
            var policy = typeof(CustomersView)
                .GetField("_featurePolicy", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view);

            Assert.IsType<Clovent.Desktop.Forms.Base.SerializedMediator>(mediator);
            Assert.IsType<Clovent.Desktop.Forms.Base.SerializedFeatureAuthorizationPolicy>(policy);
        }
    }

    /// <summary>
    /// D22, behaviourally: two independently-triggered async chains must never
    /// have work in flight against this scope at the same time, which is the
    /// condition EF Core reports as "a second operation was started on this
    /// context instance".
    /// </summary>
    [Fact]
    public async Task OverlappingOperations_NeverRunAgainstTheScopeConcurrently()
    {
        var session = new FakeCurrentSession();
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "QA User");

        var policy = new ConcurrencyTrackingFeaturePolicy();
        var mediator = new CountingMediator { Customers = [Customer("C001", "John Smith", 1m)] };

        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(policy);
        provider.Register<ILogger<CustomersView>>(NullLogger<CustomersView>.Instance);

        using var view = new CustomersView(new FakeServiceScopeFactory(provider), session);

        var refresh = typeof(CustomersView).GetMethod("RefreshAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var applyFilters = typeof(CustomersView).GetMethod("ApplyFiltersAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

        await Task.WhenAll(
            (Task)refresh.Invoke(view, [CancellationToken.None])!,
            (Task)applyFilters.Invoke(view, [])!,
            (Task)applyFilters.Invoke(view, [])!);

        Assert.Equal(1, policy.MaxObservedConcurrency);
    }

    /// <summary>Yields mid-call so any overlap between two callers is actually observable.</summary>
    private sealed class ConcurrencyTrackingFeaturePolicy : IFeatureAuthorizationPolicy
    {
        private int _inFlight;

        public int MaxObservedConcurrency { get; private set; }

        public async Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default)
        {
            var current = Interlocked.Increment(ref _inFlight);
            MaxObservedConcurrency = Math.Max(MaxObservedConcurrency, current);
            try
            {
                await Task.Yield();
                return true;
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }
    }

    private static void InvokeRefresh(CustomersView view)
    {
        var refresh = typeof(CustomersView).GetMethod(
            "RefreshAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        ((Task)refresh.Invoke(view, [CancellationToken.None])!).GetAwaiter().GetResult();
    }

    private static decimal OutstandingOf(object row) =>
        (decimal)row.GetType().GetProperty("OutstandingBalance")!.GetValue(row)!;

    private static string NameOf(object row) =>
        (string)row.GetType().GetProperty("Name")!.GetValue(row)!;
}
