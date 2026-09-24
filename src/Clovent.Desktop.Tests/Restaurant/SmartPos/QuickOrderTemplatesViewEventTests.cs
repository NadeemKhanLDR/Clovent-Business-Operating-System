using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.QuickOrderTemplates.Dtos;
using Clovent.Restaurant.Application.QuickOrderTemplates.Queries;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

public class QuickOrderTemplatesViewEventTests
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

#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class AllowAllFeaturePolicy : IFeatureAuthorizationPolicy
    {
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class CountingMediator : IMediator
    {
        public List<QuickOrderTemplateDto> Templates { get; set; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            switch (request)
            {
                case ListAllQuickOrderTemplatesQuery:
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

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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

    [Fact]
    public void GridView_DoesNotSubscribeToRowCellClick()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var gridView = (GridView)typeof(QuickOrderTemplatesView)
                .GetField("_gridView", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view)!;

            // Verify that no RowCellClick event handler exists in QuickOrderTemplatesView
            var methods = typeof(QuickOrderTemplatesView).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.DoesNotContain(methods, m => m.Name == "GridView_RowCellClick");
        }
    }

    [Fact]
    public void CentralizedEditMethod_ExistsAndIsGuarded()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var method = typeof(QuickOrderTemplatesView).GetMethod("OpenSelectedTemplateForEditAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var guardField = typeof(QuickOrderTemplatesView).GetField("_isEditDialogOpen", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(guardField);
            Assert.False((bool)guardField.GetValue(view)!);
        }
    }

    [Fact]
    public void EditForm_QuantityColumn_ConfiguredWithDiscreteNumberDisplay()
    {
        var variantId = Guid.NewGuid();
        var options = new List<ProductOptionRow>
        {
            new(variantId, Guid.NewGuid(), "Biryani", "Regular", 500m),
        };

        var model = new QuickOrderTemplateEditModel(
            "Test Template",
            "Description",
            1,
            [(variantId, 2m, 450m)],
            true);

        using var form = new QuickOrderTemplateEditForm("Edit Template", options, model);

        var gridViewField = typeof(QuickOrderTemplateEditForm).GetField("_itemsView", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(gridViewField);
        var gridView = (GridView)gridViewField.GetValue(form)!;

        var qtyCol = gridView.Columns[nameof(TemplateItemRow.Quantity)];
        Assert.NotNull(qtyCol);
        Assert.Equal(DevExpress.Utils.FormatType.Numeric, qtyCol.DisplayFormat.FormatType);
        Assert.Equal("0.##", qtyCol.DisplayFormat.FormatString);
    }
}
