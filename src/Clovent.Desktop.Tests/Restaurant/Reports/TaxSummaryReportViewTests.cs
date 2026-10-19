using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.Reports;
using Clovent.Desktop.Sessions;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Reports;

public class TaxSummaryReportViewTests
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
        private readonly IMediator _mediator = new DummyMediator();

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IMediator))
                return _mediator;
            return null;
        }
    }

    private sealed class FakeScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new FakeServiceProvider();
        public void Dispose() { }
    }

    private sealed class FakeScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new FakeScope();
    }

    private static TaxSummaryReportView CreateView()
    {
        return new TaxSummaryReportView(new FakeScopeFactory(), new FakeCurrentSession());
    }

    [Fact]
    public void TaxSummaryReportView_InitializesControlsWithProperProperties()
    {
        using var view = CreateView();

        var warehouseCombo = (ComboBoxEdit)typeof(TaxSummaryReportView)
            .GetField("_warehouseCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var periodCombo = (ComboBoxEdit)typeof(TaxSummaryReportView)
            .GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var fromDateEdit = (DateEdit)typeof(TaxSummaryReportView)
            .GetField("_fromDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var toDateEdit = (DateEdit)typeof(TaxSummaryReportView)
            .GetField("_toDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var refreshBtn = (SimpleButton)typeof(TaxSummaryReportView)
            .GetField("_refreshButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

        Assert.NotNull(warehouseCombo);
        Assert.NotNull(periodCombo);
        Assert.NotNull(fromDateEdit);
        Assert.NotNull(toDateEdit);
        Assert.NotNull(refreshBtn);
        Assert.True(refreshBtn.AutoSize);
    }

    [Fact]
    public void TaxSummaryReportView_ApplyDpiScaling_ScalesFilterControlsAndButton()
    {
        using var view = CreateView();
        view.ApplyDpiScaling();

        var warehouseCombo = (ComboBoxEdit)typeof(TaxSummaryReportView)
            .GetField("_warehouseCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var periodCombo = (ComboBoxEdit)typeof(TaxSummaryReportView)
            .GetField("_periodCombo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var fromDateEdit = (DateEdit)typeof(TaxSummaryReportView)
            .GetField("_fromDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var toDateEdit = (DateEdit)typeof(TaxSummaryReportView)
            .GetField("_toDateEdit", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        var refreshBtn = (SimpleButton)typeof(TaxSummaryReportView)
            .GetField("_refreshButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

        int expectedWarehouseW = DesktopDpi.Scale(240, view);
        int expectedPeriodW = DesktopDpi.Scale(160, view);
        int expectedDateW = DesktopDpi.Scale(140, view);
        int expectedButtonW = DesktopDpi.Scale(140, view);
        int expectedHeight = DesktopDpi.Scale(DesktopStyle.ToolbarControlHeight, view);

        Assert.Equal(expectedWarehouseW, warehouseCombo.Width);
        Assert.Equal(expectedPeriodW, periodCombo.Width);
        Assert.Equal(expectedDateW, fromDateEdit.Width);
        Assert.Equal(expectedDateW, toDateEdit.Width);
        Assert.True(refreshBtn.MinimumSize.Width >= expectedButtonW);
        Assert.True(refreshBtn.MinimumSize.Height >= expectedHeight);
    }
}
