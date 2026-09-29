using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.Customers;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Customers;

public class CustomerReceivablesReportViewTests
{
    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Accountant User";
        public bool IsAuthenticated => UserId.HasValue;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class DummyMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(TResponse)!);

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

    private sealed class AllowAllFeaturePolicy : IFeatureAuthorizationPolicy
    {
        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private static CustomerReceivablesReportView CreateView()
    {
        var session = new FakeCurrentSession();
        var mediator = new DummyMediator();
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new AllowAllFeaturePolicy());

        return new CustomerReceivablesReportView(new FakeServiceScopeFactory(provider), session);
    }

    [Fact]
    public void CustomerReceivablesReportView_FilterRow_IsTableLayoutPanelWithProperColumns()
    {
        using var view = CreateView();
        var row1Panel = (TableLayoutPanel)typeof(CustomerReceivablesReportView)
            .GetField("_row1Panel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

        Assert.NotNull(row1Panel);
        Assert.Equal(7, row1Panel.ColumnCount);

        // Verify column types:
        // Col 0: AsOf label (AutoSize)
        // Col 1: AsOfDate (Absolute)
        // Col 2: Filter label (AutoSize)
        // Col 3: FilterCombo (Absolute)
        // Col 4: Search label (AutoSize)
        // Col 5: SearchEdit (Percent 100)
        // Col 6: Refresh button (AutoSize)
        Assert.Equal(SizeType.AutoSize, row1Panel.ColumnStyles[0].SizeType);
        Assert.Equal(SizeType.Absolute, row1Panel.ColumnStyles[1].SizeType);
        Assert.Equal(SizeType.AutoSize, row1Panel.ColumnStyles[2].SizeType);
        Assert.Equal(SizeType.Absolute, row1Panel.ColumnStyles[3].SizeType);
        Assert.Equal(SizeType.AutoSize, row1Panel.ColumnStyles[4].SizeType);
        Assert.Equal(SizeType.Percent, row1Panel.ColumnStyles[5].SizeType);
        Assert.Equal(SizeType.AutoSize, row1Panel.ColumnStyles[6].SizeType);
    }

    [Fact]
    public void CustomerReceivablesReportView_ScaleLayoutAtRuntime_AppliesDpiScaling()
    {
        using var view = CreateView();
        view.ScaleLayoutAtRuntime();

        var asOfEdit = (DateEdit)typeof(CustomerReceivablesReportView)
            .GetField("_asOfDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var filterCombo = (ComboBoxEdit)typeof(CustomerReceivablesReportView)
            .GetField("_filterCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var searchEdit = (TextEdit)typeof(CustomerReceivablesReportView)
            .GetField("_searchEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var refreshBtn = (SimpleButton)typeof(CustomerReceivablesReportView)
            .GetField("_refreshButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var gridView = (GridView)typeof(CustomerReceivablesReportView)
            .GetField("_gridView", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

        int expectedAsOfW = DesktopDpi.Scale(150, view);
        int expectedFilterW = DesktopDpi.Scale(180, view);
        int expectedSearchMinW = DesktopDpi.Scale(340, view);
        int expectedRefreshMinW = DesktopDpi.Scale(100, view);
        int expectedRowH = DesktopDpi.Scale(28, view);
        int expectedColHeaderH = DesktopDpi.Scale(32, view);

        Assert.True(asOfEdit.MinimumSize.Width >= expectedAsOfW);
        Assert.True(filterCombo.MinimumSize.Width >= expectedFilterW);
        Assert.True(searchEdit.MinimumSize.Width >= expectedSearchMinW);
        Assert.True(refreshBtn.MinimumSize.Width >= expectedRefreshMinW);

        Assert.Equal(expectedRowH, gridView.RowHeight);
        Assert.Equal(expectedColHeaderH, gridView.ColumnPanelRowHeight);
    }

    [Fact]
    public void CustomerReceivablesReportView_StatCards_AreConfigured()
    {
        using var view = CreateView();
        var totalRecLabel = (LabelControl)typeof(CustomerReceivablesReportView)
            .GetField("_totalReceivablesLabel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var totalAdvLabel = (LabelControl)typeof(CustomerReceivablesReportView)
            .GetField("_totalAdvancesLabel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var totalOverLimitLabel = (LabelControl)typeof(CustomerReceivablesReportView)
            .GetField("_totalOverLimitLabel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var accountsWithBalLabel = (LabelControl)typeof(CustomerReceivablesReportView)
            .GetField("_accountsWithBalanceLabel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

        Assert.NotNull(totalRecLabel);
        Assert.NotNull(totalAdvLabel);
        Assert.NotNull(totalOverLimitLabel);
        Assert.NotNull(accountsWithBalLabel);
    }
}
