using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Clovent.Restaurant.Application.QuickOrderTemplates.Queries;
using DevExpress.XtraGrid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

/// <summary>
/// Load/refresh coverage for <see cref="QuickOrderTemplatesView"/>, following
/// the CustomersView load-and-refresh test pattern (headless form driving),
/// plus pricing behavior of the edit dialog's item rows.
/// </summary>
public class QuickOrderTemplatesViewLoadAndRefreshTests
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

    /// <summary>Answers the all-templates list and the catalog option queries the view's load path sends.</summary>
    private sealed class CountingMediator : IMediator
    {
        public List<QuickOrderTemplateDto> Templates { get; set; } = [];

        public int ListAllCallCount { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            switch (request)
            {
                case ListAllQuickOrderTemplatesQuery:
                    ListAllCallCount++;
                    return Task.FromResult((TResponse)(object)(IReadOnlyList<QuickOrderTemplateDto>)[.. Templates]);
                case ListProductVariantsQuery:
                    return Task.FromResult((TResponse)(object)(IReadOnlyList<Clovent.Catalog.Application.Variants.Dtos.ProductVariantDto>)[]);
                case ListProductsQuery:
                    return Task.FromResult((TResponse)(object)(IReadOnlyList<Clovent.Catalog.Application.Products.Dtos.ProductDto>)[]);
                case ListActiveProductPricesByTypeQuery:
                    return Task.FromResult((TResponse)(object)(IReadOnlyList<Clovent.Catalog.Application.Prices.Dtos.ProductPriceDto>)[]);
                default:
                    return Task.FromResult(default(TResponse)!);
            }
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

    private static readonly QuickOrderTemplateItemDto[] NoItems = [];

    private static QuickOrderTemplateDto Template(string name, bool isActive) => new(
        TemplateId: Guid.NewGuid(),
        Name: name,
        Description: null,
        IsActive: isActive,
        DisplayOrder: 1,
        Items: NoItems,
        TotalPrice: 0m);

    private static (QuickOrderTemplatesView View, CountingMediator Mediator) CreateView()
    {
        var session = new FakeCurrentSession();
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "QA User");

        var mediator = new CountingMediator();

        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new AllowAllFeaturePolicy());
        provider.Register<ILogger<QuickOrderTemplatesView>>(NullLogger<QuickOrderTemplatesView>.Instance);

        return (new QuickOrderTemplatesView(new FakeServiceScopeFactory(provider), session), mediator);
    }

    private static List<object> GridRows(QuickOrderTemplatesView view)
    {
        var gridControl = (GridControl)typeof(QuickOrderTemplatesView)
            .GetField("_gridControl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(view)!;

        return gridControl.DataSource is System.Collections.IEnumerable rows
            ? [.. rows.Cast<object>()]
            : [];
    }

    private static void RaiseRefreshClick(QuickOrderTemplatesView view)
    {
        var button = (DevExpress.XtraEditors.SimpleButton)typeof(QuickOrderTemplatesView)
            .GetField("_refreshButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(view)!;
        button.PerformClick();
    }

    [Fact]
    public void CreatingTheControl_RaisesLoadAndFetchesAllTemplates_IncludingInactive()
    {
        var (view, mediator) = CreateView();
        using (view)
        {
            mediator.Templates = [Template("Active One", true), Template("Hidden", false)];

            view.CreateControl();

            Assert.Equal(1, mediator.ListAllCallCount);
            Assert.Equal(2, GridRows(view).Count);
        }
    }

    [Fact]
    public void RefreshButton_ReReadsTheTemplatesInsteadOfReusingTheCache()
    {
        var (view, mediator) = CreateView();
        using (view)
        {
            view.CreateControl();

            mediator.Templates = [Template("A", true), Template("B", true), Template("C", false)];
            RaiseRefreshClick(view);

            Assert.Equal(2, mediator.ListAllCallCount);
            Assert.Equal(3, GridRows(view).Count);
        }
    }

    [Fact]
    public void TemplateItemRow_OverrideWinsOverCatalogPrice_QuantityMultiplies()
    {
        var variantId = Guid.NewGuid();
        var options = new List<ProductOptionRow>
        {
            new(variantId, Guid.NewGuid(), "Burger", "Regular", 500m),
        };

        var catalogRow = new TemplateItemRow(variantId, 2m, 0m, options);
        Assert.Equal(500m, catalogRow.EffectiveUnitPrice);
        Assert.Equal(1000m, catalogRow.EffectiveTotal);

        var overrideRow = new TemplateItemRow(variantId, 2m, 450m, options);
        Assert.Equal(450m, overrideRow.EffectiveUnitPrice);
        Assert.Equal(900m, overrideRow.EffectiveTotal);
    }
}
