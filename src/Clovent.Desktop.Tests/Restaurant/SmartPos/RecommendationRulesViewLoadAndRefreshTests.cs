using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.Application.SmartRecommendations.Queries;
using DevExpress.XtraGrid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

/// <summary>
/// Load/refresh coverage for <see cref="RecommendationRulesView"/>, following
/// the CustomersView load-and-refresh test pattern (headless form driving).
/// </summary>
public class RecommendationRulesViewLoadAndRefreshTests
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

    /// <summary>Answers the rules list and the catalog option queries the view's load path sends.</summary>
    private sealed class CountingMediator : IMediator
    {
        public List<RecommendationRuleDto> Rules { get; set; } = [];

        public int ListRulesCallCount { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            switch (request)
            {
                case ListRecommendationRulesQuery:
                    ListRulesCallCount++;
                    return Task.FromResult((TResponse)(object)(IReadOnlyList<RecommendationRuleDto>)[.. Rules]);
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

    private static RecommendationRuleDto Rule(int priority, bool isActive = true) => new(
        RuleId: Guid.NewGuid(),
        ProductId: null,
        RecommendedVariantId: Guid.NewGuid(),
        Priority: priority,
        IsActive: isActive,
        StartTime: null,
        EndTime: null,
        DaysOfWeek: null,
        Notes: null);

    private static (RecommendationRulesView View, CountingMediator Mediator) CreateView()
    {
        var session = new FakeCurrentSession();
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "QA User");

        var mediator = new CountingMediator();

        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new AllowAllFeaturePolicy());
        provider.Register<ILogger<RecommendationRulesView>>(NullLogger<RecommendationRulesView>.Instance);

        return (new RecommendationRulesView(new FakeServiceScopeFactory(provider), session), mediator);
    }

    private static List<object> GridRows(RecommendationRulesView view)
    {
        var gridControl = (GridControl)typeof(RecommendationRulesView)
            .GetField("_gridControl", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(view)!;

        return gridControl.DataSource is System.Collections.IEnumerable rows
            ? [.. rows.Cast<object>()]
            : [];
    }

    private static void RaiseRefreshClick(RecommendationRulesView view)
    {
        var button = (DevExpress.XtraEditors.SimpleButton)typeof(RecommendationRulesView)
            .GetField("_refreshButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(view)!;
        button.PerformClick();
    }

    [Fact]
    public void CreatingTheControl_RaisesLoadAndFetchesRules()
    {
        var (view, mediator) = CreateView();
        using (view)
        {
            mediator.Rules = [Rule(1), Rule(2)];

            view.CreateControl();

            Assert.Equal(1, mediator.ListRulesCallCount);
            Assert.Equal(2, GridRows(view).Count);
        }
    }

    [Fact]
    public void RefreshButton_ReReadsTheRulesInsteadOfReusingTheCache()
    {
        var (view, mediator) = CreateView();
        using (view)
        {
            view.CreateControl();

            mediator.Rules = [Rule(1), Rule(2), Rule(3)];
            RaiseRefreshClick(view);

            Assert.Equal(2, mediator.ListRulesCallCount);
            Assert.Equal(3, GridRows(view).Count);
        }
    }

    [Theory]
    [InlineData(null, "Every day")]
    [InlineData(0b0111_1111, "Every day")]
    [InlineData(0b0100_0000, "Sat")]
    [InlineData(0b0000_0011, "Sun, Mon")]
    [InlineData(0, "No days")]
    public void FormatDays_RendersBitmaskAsReadableCaption(int? daysOfWeek, string expected)
    {
        Assert.Equal(expected, RecommendationRulesView.FormatDays(daysOfWeek));
    }
}
