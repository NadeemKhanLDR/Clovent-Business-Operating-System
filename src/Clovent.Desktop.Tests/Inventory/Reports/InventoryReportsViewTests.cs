using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Inventory.Reports;
using Clovent.Desktop.Sessions;
using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Queries;
using DevExpress.XtraTab;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Inventory.Reports;

public class InventoryReportsViewTests
{
    private sealed class FakeSession : ICurrentSession
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Inventory Auditor";
        public bool IsAuthenticated => UserId.HasValue;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
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

    private sealed class ReportingTestMediator : IMediator
    {
        public List<WarehouseDto> Warehouses { get; set; } =
        [
            new(Guid.NewGuid(), Guid.NewGuid(), "Central Store", "MAIN", "Active", DateTimeOffset.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), "Kitchen Store", "KIT", "Active", DateTimeOffset.UtcNow)
        ];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ListAllWarehousesQuery)
            {
                return Task.FromResult((TResponse)(object)Warehouses.AsReadOnly());
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

    [Fact]
    public void InventoryReportsView_InitializesWithFiveDistinctTabs()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMediator>(new ReportingTestMediator());
        var provider = services.BuildServiceProvider();
        var scopeFactory = new FakeServiceScopeFactory(provider);
        var session = new FakeSession();

        using var view = new InventoryReportsView(scopeFactory, session);

        Assert.NotNull(view);

        // Find the XtraTabControl inside view
        var tabControl = FindControl<XtraTabControl>(view);
        Assert.NotNull(tabControl);
        Assert.Equal(5, tabControl.TabPages.Count);

        var tabTitles = new List<string>();
        foreach (XtraTabPage page in tabControl.TabPages)
        {
            tabTitles.Add(page.Text);
        }

        Assert.Contains(tabTitles, t => t.Contains("Recipe Food Cost"));
        Assert.Contains(tabTitles, t => t.Contains("Ingredient Purchasing Costs"));
        Assert.Contains(tabTitles, t => t.Contains("Asset Valuation"));
        Assert.Contains(tabTitles, t => t.Contains("Stock Variance"));
        Assert.Contains(tabTitles, t => t.Contains("Spoilage & Wastage"));
    }

    private static T? FindControl<T>(System.Windows.Forms.Control root) where T : System.Windows.Forms.Control
    {
        if (root is T match) return match;
        foreach (System.Windows.Forms.Control child in root.Controls)
        {
            var found = FindControl<T>(child);
            if (found != null) return found;
        }
        return null;
    }
}
