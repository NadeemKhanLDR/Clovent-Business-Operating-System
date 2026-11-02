using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Desktop.Forms.Base;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Catalog.Prices;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.Currencies.Dtos;
using Clovent.MasterData.Application.Currencies.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Catalog.Prices;

public class ProductPriceManagementViewConcurrencyTests
{
    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Catalog Manager";
        public bool IsAuthenticated => UserId.HasValue;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class RacyScopeTracker
    {
        private int _inFlight;
        public int MaxConcurrent { get; private set; }

        public async Task EnterAsync()
        {
            var count = Interlocked.Increment(ref _inFlight);
            lock (this)
            {
                if (count > MaxConcurrent)
                {
                    MaxConcurrent = count;
                }
            }

            if (count > 1)
            {
                Interlocked.Decrement(ref _inFlight);
                throw new InvalidOperationException("A second operation was started on this context instance before a previous operation completed.");
            }

            try
            {
                await Task.Delay(15);
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }
    }

    private sealed class RacyPriceMediator(RacyScopeTracker scope) : IMediator
    {
        public List<ProductVariantDto> Variants { get; set; } = [];
        public List<ProductPriceDto> Prices { get; set; } = [];
        public List<CurrencyDto> Currencies { get; set; } = [];

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            await scope.EnterAsync();

            if (request is ListProductVariantsQuery)
            {
                return (TResponse)(object)Variants.AsReadOnly();
            }

            if (request is ListProductPricesByVariantQuery pq)
            {
                var matching = Prices.Where(p => p.ProductVariantId == pq.ProductVariantId).ToList();
                return (TResponse)(object)matching.AsReadOnly();
            }

            if (request is ListCurrenciesQuery)
            {
                return (TResponse)(object)Currencies.AsReadOnly();
            }

            return default!;
        }

        public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            await scope.EnterAsync();

        public async Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            await scope.EnterAsync();
            return null;
        }

        public async Task Publish(object notification, CancellationToken cancellationToken = default) =>
            await scope.EnterAsync();

        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
            await scope.EnterAsync();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = [];
        public void Register<T>(T service) where T : class => _services[typeof(T)] = service;
        public object? GetService(Type serviceType) => _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    private sealed class FakeServiceScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public void Dispose() { }
    }

    private sealed class FakeServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FakeServiceScope(serviceProvider);
    }

    private sealed class RacyFeaturePolicy(RacyScopeTracker scope) : IFeatureAuthorizationPolicy
    {
        public async Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default)
        {
            await scope.EnterAsync();
            return true;
        }
    }

    [Fact]
    public async Task ProductPriceManagementView_InitialLoad_ExecutesWithoutContextCollision()
    {
        var session = new FakeCurrentSession();
        var tracker = new RacyScopeTracker();
        var mediator = new RacyPriceMediator(tracker);
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new RacyFeaturePolicy(tracker));

        var variantId1 = Guid.NewGuid();
        var variantId2 = Guid.NewGuid();
        var productId = Guid.NewGuid();

        mediator.Variants.Add(new ProductVariantDto(variantId1, productId, "Naan Standard", "NAAN-01", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active", true, "Prepared", "Naan"));
        mediator.Variants.Add(new ProductVariantDto(variantId2, productId, "Roti Standard", "ROTI-01", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active", true, "Prepared", "Roti"));

        mediator.Prices.Add(new ProductPriceDto(Guid.NewGuid(), variantId1, "Retail", 100m, Guid.NewGuid(), DateTimeOffset.UtcNow, "Active", DateTimeOffset.UtcNow));
        mediator.Prices.Add(new ProductPriceDto(Guid.NewGuid(), variantId2, "Retail", 50m, Guid.NewGuid(), DateTimeOffset.UtcNow, "Active", DateTimeOffset.UtcNow));

        var view = new ProductPriceManagementView(new FakeServiceScopeFactory(provider), session);
        using (view)
        {
            var loadMethod = typeof(ProductPriceManagementView).GetMethod("LoadVariantsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(loadMethod);
            await (Task)loadMethod.Invoke(view, null)!;

            Assert.Equal(1, tracker.MaxConcurrent);

            var pickerField = typeof(ProductPriceManagementView).GetField("_variantPicker", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(pickerField);
            var picker = (EntityPicker)pickerField.GetValue(view)!;
            Assert.Equal(variantId1, picker.SelectedId);
        }
    }

    [Fact]
    public async Task ProductPriceManagementView_RapidVariantChanges_CoalescesAndMaintainsLatestSelection()
    {
        var session = new FakeCurrentSession();
        var tracker = new RacyScopeTracker();
        var mediator = new RacyPriceMediator(tracker);
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new RacyFeaturePolicy(tracker));

        var variantId1 = Guid.NewGuid();
        var variantId2 = Guid.NewGuid();
        var variantId3 = Guid.NewGuid();
        var productId = Guid.NewGuid();

        mediator.Variants.Add(new ProductVariantDto(variantId1, productId, "Item 1", "SKU-1", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active", true, "Category", "Product 1"));
        mediator.Variants.Add(new ProductVariantDto(variantId2, productId, "Item 2", "SKU-2", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active", true, "Category", "Product 2"));
        mediator.Variants.Add(new ProductVariantDto(variantId3, productId, "Item 3", "SKU-3", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active", true, "Category", "Product 3"));

        mediator.Prices.Add(new ProductPriceDto(Guid.NewGuid(), variantId1, "Selling", 10m, Guid.NewGuid(), DateTimeOffset.UtcNow, "Active", DateTimeOffset.UtcNow));
        mediator.Prices.Add(new ProductPriceDto(Guid.NewGuid(), variantId2, "Selling", 20m, Guid.NewGuid(), DateTimeOffset.UtcNow, "Active", DateTimeOffset.UtcNow));
        mediator.Prices.Add(new ProductPriceDto(Guid.NewGuid(), variantId3, "Selling", 30m, Guid.NewGuid(), DateTimeOffset.UtcNow, "Active", DateTimeOffset.UtcNow));

        var view = new ProductPriceManagementView(new FakeServiceScopeFactory(provider), session);
        using (view)
        {
            var loadMethod = typeof(ProductPriceManagementView).GetMethod("LoadVariantsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(loadMethod);
            await (Task)loadMethod.Invoke(view, null)!;

            var pickerField = typeof(ProductPriceManagementView).GetField("_variantPicker", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(pickerField);
            var picker = (EntityPicker)pickerField.GetValue(view)!;

            var listViewField = typeof(ProductPriceManagementView).GetField("_listView", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(listViewField);
            var listView = listViewField.GetValue(view)!;
            var refreshMethod = listView.GetType().GetMethod("RefreshAsync", [typeof(CancellationToken)]);
            Assert.NotNull(refreshMethod);

            // Simulate rapid user selection switches
            picker.SelectId(variantId2);
            var task1 = (Task)refreshMethod.Invoke(listView, [CancellationToken.None])!;

            picker.SelectId(variantId3);
            var task2 = (Task)refreshMethod.Invoke(listView, [CancellationToken.None])!;

            await Task.WhenAll(task1, task2);

            Assert.Equal(1, tracker.MaxConcurrent);
            Assert.Equal(variantId3, picker.SelectedId);
        }
    }

    [Fact]
    public async Task ProductPriceManagementView_OverlappingRefreshAndCurrencyQuery_SerializesCleanly()
    {
        var session = new FakeCurrentSession();
        var tracker = new RacyScopeTracker();
        var mediator = new RacyPriceMediator(tracker);
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new RacyFeaturePolicy(tracker));

        var variantId = Guid.NewGuid();
        mediator.Variants.Add(new ProductVariantDto(variantId, Guid.NewGuid(), "Item", "SKU", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active", true, "Category", "Product"));
        mediator.Currencies.Add(new CurrencyDto(Guid.NewGuid(), "PKR", "Pakistani Rupee", "Rs", 2, "Active", DateTimeOffset.UtcNow));

        var view = new ProductPriceManagementView(new FakeServiceScopeFactory(provider), session);
        using (view)
        {
            var loadMethod = typeof(ProductPriceManagementView).GetMethod("LoadVariantsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(loadMethod);
            await (Task)loadMethod.Invoke(view, null)!;

            var listViewField = typeof(ProductPriceManagementView).GetField("_listView", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(listViewField);
            var listView = listViewField.GetValue(view)!;
            var refreshMethod = listView.GetType().GetMethod("RefreshAsync", [typeof(CancellationToken)]);
            Assert.NotNull(refreshMethod);

            var loadCurrencyMethod = typeof(ProductPriceManagementView).GetMethod("LoadCurrencyOptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(loadCurrencyMethod);

            // Launch refresh and currency options concurrently
            var t1 = (Task)refreshMethod.Invoke(listView, [CancellationToken.None])!;
            var t2 = (Task<IReadOnlyList<(Guid Id, string Display)>>)loadCurrencyMethod.Invoke(view, null)!;

            await Task.WhenAll(t1, t2);

            Assert.Equal(1, tracker.MaxConcurrent);
            Assert.NotEmpty(await t2);
        }
    }

    [Fact]
    public async Task ProductPriceManagementView_DisposalDuringActiveRefresh_CancelsAndDrainsWithoutScopeDisposedExceptions()
    {
        var session = new FakeCurrentSession();
        var tracker = new RacyScopeTracker();
        var mediator = new RacyPriceMediator(tracker);
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new RacyFeaturePolicy(tracker));

        var variantId = Guid.NewGuid();
        mediator.Variants.Add(new ProductVariantDto(variantId, Guid.NewGuid(), "Item", "SKU", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active", true, "Category", "Product"));

        var view = new ProductPriceManagementView(new FakeServiceScopeFactory(provider), session);

        var loadMethod = typeof(ProductPriceManagementView).GetMethod("LoadVariantsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(loadMethod);
        await (Task)loadMethod.Invoke(view, null)!;

        var listViewField = typeof(ProductPriceManagementView).GetField("_listView", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(listViewField);
        var listView = listViewField.GetValue(view)!;
        var refreshMethod = listView.GetType().GetMethod("RefreshAsync", [typeof(CancellationToken)]);
        Assert.NotNull(refreshMethod);

        var refreshTask = (Task)refreshMethod.Invoke(listView, [CancellationToken.None])!;

        // Dispose view while refresh is actively in flight
        view.Dispose();

        await refreshTask;
        Assert.Equal(1, tracker.MaxConcurrent);
    }
    private sealed class TrackingScope(IServiceProvider provider, Action onDispose) : IServiceScope
    {
        public IServiceProvider ServiceProvider => provider;
        public void Dispose() => onDispose();
    }

    private sealed class TrackingScopeFactory(IServiceScope scope) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => scope;
    }

    [Fact]
    public async Task ProductPriceManagementView_DelayedOperationExceeding500msDuringDisposal_DoesNotDisposeScopePrematurelyOrDeadlock()
    {
        var session = new FakeCurrentSession();
        var scopeDisposed = false;
        var operationCompleted = false;

        var tracker = new RacyScopeTracker();
        var mediator = new RacyPriceMediator(tracker);
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new RacyFeaturePolicy(tracker));

        var variantId = Guid.NewGuid();
        mediator.Variants.Add(new ProductVariantDto(variantId, Guid.NewGuid(), "Item", "SKU", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active", true, "Category", "Product"));

        // Custom scope tracking disposal
        var scope = new TrackingScope(provider, () => scopeDisposed = true);
        var scopeFactory = new TrackingScopeFactory(scope);

        var view = new ProductPriceManagementView(scopeFactory, session);

        var loadMethod = typeof(ProductPriceManagementView).GetMethod("LoadVariantsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(loadMethod);
        await (Task)loadMethod.Invoke(view, null)!;

        // Obtain gate and run an un-cancelable slow operation that exceeds the 500ms Drain timeout
        var gateField = typeof(ProductPriceManagementView).GetField("_gate", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(gateField);
        var gate = (ScreenOperationGate)gateField.GetValue(view)!;

        var longRunningTask = gate.RunAsync(async () =>
        {
            // Simulates an operation that takes 700ms (exceeding 500ms drain timeout)
            await Task.Delay(700);
            Assert.False(scopeDisposed, "Scope must NOT be disposed while long-running operation is still using it!");
            operationCompleted = true;
        });

        // Small delay to ensure the gate has been acquired by the long-running task
        await Task.Delay(50);

        // Dispose view while operation is in flight - Drain(500ms) will time out
        var sw = System.Diagnostics.Stopwatch.StartNew();
        view.Dispose();
        sw.Stop();

        // Dispose returns after ~500ms without deadlocking
        Assert.True(sw.ElapsedMilliseconds >= 450 && sw.ElapsedMilliseconds < 2000, $"Dispose took {sw.ElapsedMilliseconds}ms");
        Assert.False(scopeDisposed, "Scope must remain alive immediately after timeout");

        // Wait for the background operation to complete
        await longRunningTask;
        Assert.True(operationCompleted);

        // Scope will be safely disposed in background task once the gate is released
        var scopeDisposedEventually = false;
        for (int i = 0; i < 50; i++)
        {
            if (scopeDisposed) { scopeDisposedEventually = true; break; }
            await Task.Delay(50);
        }
        Assert.True(scopeDisposedEventually, "Scope must eventually be disposed after gate clears");
    }

    [Fact]
    public async Task ProductPriceManagementView_AllMutationsAndQueries_AreRoutedThroughGate()
    {
        var session = new FakeCurrentSession();
        var tracker = new RacyScopeTracker();
        var mediator = new RacyPriceMediator(tracker);
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new RacyFeaturePolicy(tracker));

        var variantId = Guid.NewGuid();
        var priceId = Guid.NewGuid();
        mediator.Variants.Add(new ProductVariantDto(variantId, Guid.NewGuid(), "Item", "SKU", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow, null, "Active", true, "Category", "Product"));
        mediator.Prices.Add(new ProductPriceDto(priceId, variantId, "Retail", 100m, Guid.NewGuid(), DateTimeOffset.UtcNow, "Active", DateTimeOffset.UtcNow));
        mediator.Currencies.Add(new CurrencyDto(Guid.NewGuid(), "PKR", "Pakistani Rupee", "Rs", 2, "Active", DateTimeOffset.UtcNow));

        var view = new ProductPriceManagementView(new FakeServiceScopeFactory(provider), session);
        using (view)
        {
            var loadMethod = typeof(ProductPriceManagementView).GetMethod("LoadVariantsAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(loadMethod);
            await (Task)loadMethod.Invoke(view, null)!;

            var listViewField = typeof(ProductPriceManagementView).GetField("_listView", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(listViewField);
            var listView = listViewField.GetValue(view)!;
            var refreshMethod = listView.GetType().GetMethod("RefreshAsync", [typeof(CancellationToken)]);
            Assert.NotNull(refreshMethod);

            var canUseFeatureMethod = typeof(ProductPriceManagementView).GetMethod("CanUseFeatureAsync", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(canUseFeatureMethod);

            var editDto = mediator.Prices[0];
            var onActivate = (Func<ProductPriceDto, Task>)listView.GetType().GetProperty("OnActivate")!.GetValue(listView)!;
            var onDeactivate = (Func<ProductPriceDto, Task>)listView.GetType().GetProperty("OnDeactivate")!.GetValue(listView)!;

            // Launch concurrent mutation, query, authorization, activation, deactivation
            var tasks = new List<Task>
            {
                (Task)refreshMethod.Invoke(listView, [CancellationToken.None])!,
                (Task<bool>)canUseFeatureMethod.Invoke(view, ["create"])!,
                onActivate(editDto),
                onDeactivate(editDto)
            };

            await Task.WhenAll(tasks);
            Assert.Equal(1, tracker.MaxConcurrent);
        }
    }
}
