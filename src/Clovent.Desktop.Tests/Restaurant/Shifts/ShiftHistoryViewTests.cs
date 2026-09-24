using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.Shifts;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Application.Shifts.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Shifts;

public class ShiftHistoryViewTests
{
    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? SessionId { get; set; } = Guid.NewGuid();
        public string? DisplayName { get; set; } = "Manager User";
        public bool IsAuthenticated => UserId.HasValue;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
#pragma warning disable CS0067
        public event EventHandler? Changed;
#pragma warning restore CS0067
    }

    private sealed class ShiftTestMediator : IMediator
    {
        public List<ShiftDto> Shifts { get; } = [];
        public ListShiftsQuery? LastQuery { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ListShiftsQuery query)
            {
                LastQuery = query;
                return Task.FromResult((TResponse)(object)(IReadOnlyList<ShiftDto>)Shifts);
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

    private static (ShiftHistoryView View, ShiftTestMediator Mediator) CreateView()
    {
        var session = new FakeCurrentSession();
        var mediator = new ShiftTestMediator();
        var provider = new FakeServiceProvider();
        provider.Register<IMediator>(mediator);
        provider.Register<IFeatureAuthorizationPolicy>(new AllowAllFeaturePolicy());

        var view = new ShiftHistoryView(new FakeServiceScopeFactory(provider), session, Microsoft.Extensions.Logging.Abstractions.NullLogger<ShiftHistoryView>.Instance);
        return (view, mediator);
    }

    [Fact]
    public void ShiftHistoryView_HasRequiredFilterControlsAndActionButtons()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var periodCombo = (ComboBoxEdit)typeof(ShiftHistoryView).GetField("_cboPeriod", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var fromDate = (DateEdit)typeof(ShiftHistoryView).GetField("_dtFrom", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var toDate = (DateEdit)typeof(ShiftHistoryView).GetField("_dtTo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var statusCombo = (ComboBoxEdit)typeof(ShiftHistoryView).GetField("_cboStatus", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var btnSearch = (SimpleButton)typeof(ShiftHistoryView).GetField("_btnSearch", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var btnClear = (SimpleButton)typeof(ShiftHistoryView).GetField("_btnClear", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            var btnOpenShift = (SimpleButton)typeof(ShiftHistoryView).GetField("_btnOpenShift", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var btnCashMovement = (SimpleButton)typeof(ShiftHistoryView).GetField("_btnCashMovement", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var btnCloseShift = (SimpleButton)typeof(ShiftHistoryView).GetField("_btnCloseShift", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var btnViewDetails = (SimpleButton)typeof(ShiftHistoryView).GetField("_btnViewDetails", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            Assert.NotNull(periodCombo);
            Assert.NotNull(fromDate);
            Assert.NotNull(toDate);
            Assert.NotNull(statusCombo);
            Assert.NotNull(btnSearch);
            Assert.NotNull(btnClear);

            Assert.NotNull(btnOpenShift);
            Assert.NotNull(btnCashMovement);
            Assert.NotNull(btnCloseShift);
            Assert.NotNull(btnViewDetails);

            // Default period must be "This Month"
            Assert.Equal("This Month", periodCombo.SelectedItem?.ToString());

            // Button text must be "Cash In / Cash Out" without truncation
            Assert.Equal("Cash In / Cash Out", btnCashMovement.Text);
        }
    }

    [Fact]
    public void ShiftHistoryView_ChangingPeriodUpdatesDates_AndManualDateEditSwitchesToCustom()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var periodCombo = (ComboBoxEdit)typeof(ShiftHistoryView).GetField("_cboPeriod", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var fromDate = (DateEdit)typeof(ShiftHistoryView).GetField("_dtFrom", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var toDate = (DateEdit)typeof(ShiftHistoryView).GetField("_dtTo", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            // Select "Today"
            periodCombo.SelectedItem = "Today";
            Assert.Equal(DateTime.Today, fromDate.DateTime.Date);
            Assert.Equal(DateTime.Today, toDate.DateTime.Date);

            // Select "Yesterday"
            periodCombo.SelectedItem = "Yesterday";
            Assert.Equal(DateTime.Today.AddDays(-1), fromDate.DateTime.Date);
            Assert.Equal(DateTime.Today.AddDays(-1), toDate.DateTime.Date);

            // Manually changing date must switch period to "Custom"
            fromDate.DateTime = DateTime.Today.AddDays(-10);
            Assert.Equal("Custom", periodCombo.SelectedItem?.ToString());
        }
    }

    [Fact]
    public void ShiftHistoryView_ClearFiltersResetsToDefaults()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var periodCombo = (ComboBoxEdit)typeof(ShiftHistoryView).GetField("_cboPeriod", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var statusCombo = (ComboBoxEdit)typeof(ShiftHistoryView).GetField("_cboStatus", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            var btnClear = (SimpleButton)typeof(ShiftHistoryView).GetField("_btnClear", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

            // Change values
            periodCombo.SelectedItem = "Yesterday";
            statusCombo.SelectedIndex = 1;

            // Click clear filters
            btnClear.PerformClick();

            Assert.Equal("This Month", periodCombo.SelectedItem?.ToString());
            Assert.Equal(0, statusCombo.SelectedIndex);
        }
    }

    [Fact]
    public void ShiftHistoryView_LayoutIsResponsiveWithNoClipping()
    {
        var (view, _) = CreateView();
        using (view)
        {
            // Test at various resolutions
            int[] widths = [1024, 1366, 1920];
            int[] heights = [768, 768, 1080];

            for (int i = 0; i < widths.Length; i++)
            {
                view.Size = new Size(widths[i], heights[i]);
                view.CreateControl();
                view.PerformLayout();

                var filterPanel = (FlowLayoutPanel)typeof(ShiftHistoryView).GetField("_filterPanel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
                var grid = (GridControl)typeof(ShiftHistoryView).GetField("_gridControl", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
                var actionBar = (FlowLayoutPanel)typeof(ShiftHistoryView).GetField("_actionBar", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

                Assert.NotNull(filterPanel);
                Assert.NotNull(grid);
                Assert.NotNull(actionBar);

                // Ensure bottom panel is completely visible and within view
                Assert.True(actionBar.Bottom <= view.Height, $"Action panel bottom ({actionBar.Bottom}) exceeds view height ({view.Height}) at {widths[i]}x{heights[i]}");
            }
        }
    }

    [Fact]
    public void ShiftHistoryView_NoDuplicateTodayYesterdayButtons_AndPeriodDropdownFunctional()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var periodCombo = (ComboBoxEdit)typeof(ShiftHistoryView).GetField("_cboPeriod", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            Assert.NotNull(periodCombo);

            var items = periodCombo.Properties.Items.Cast<object>().Select(o => o.ToString()).ToList();
            Assert.Contains("Today", items);
            Assert.Contains("Yesterday", items);
            Assert.Contains("This Week", items);
            Assert.Contains("Last Week", items);
            Assert.Contains("This Month", items);
            Assert.Contains("Last Month", items);
            Assert.Contains("This Quarter", items);
            Assert.Contains("Last Quarter", items);
            Assert.Contains("This Year", items);
            Assert.Contains("Last Year", items);
            Assert.Contains("Custom", items);

            // Verify no shortcut buttons named Today or Yesterday exist anywhere in the control hierarchy
            var allControls = GetAllControls(view);
            var buttons = allControls.OfType<SimpleButton>().ToList();
            Assert.DoesNotContain(buttons, b => b.Text == "Today");
            Assert.DoesNotContain(buttons, b => b.Text == "Yesterday");
        }
    }

    private static List<Control> GetAllControls(Control root)
    {
        var list = new List<Control>();
        foreach (Control c in root.Controls)
        {
            list.Add(c);
            list.AddRange(GetAllControls(c));
        }
        return list;
    }

    /// <summary>
    /// Verifies that the Shift History grid only exposes business-relevant columns
    /// and hides all technical GUID ID columns from the default manager view.
    /// </summary>
    [Fact]
    public void ShiftHistoryView_GridProjection_HidesTechnicalGuidColumns()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var gridView = (GridView)typeof(ShiftHistoryView)
                .GetField("_gridView", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view)!;

            Assert.NotNull(gridView);

            // All visible column field names
            var visibleFieldNames = gridView.Columns
                .Cast<DevExpress.XtraGrid.Columns.GridColumn>()
                .Where(c => c.Visible)
                .Select(c => c.FieldName)
                .ToList();

            // Technical GUID IDs must NOT appear as visible columns
            Assert.DoesNotContain("ShiftId",    visibleFieldNames);
            Assert.DoesNotContain("BranchId",   visibleFieldNames);
            Assert.DoesNotContain("WarehouseId", visibleFieldNames);
            Assert.DoesNotContain("TerminalId", visibleFieldNames);
            Assert.DoesNotContain("CashierId",  visibleFieldNames);

            // Business-relevant columns MUST be present and visible
            Assert.Contains("ShiftNumber",  visibleFieldNames);
            Assert.Contains("CashierName",  visibleFieldNames);
            Assert.Contains("OpenedAtUtc",  visibleFieldNames);
            Assert.Contains("Status",       visibleFieldNames);
        }
    }

    [Fact]
    public void ShiftHistoryView_GridColumns_HaveCorrectBusinessCaptions()
    {
        var (view, _) = CreateView();
        using (view)
        {
            var gridView = (GridView)typeof(ShiftHistoryView)
                .GetField("_gridView", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view)!;

            var columnByField = gridView.Columns
                .Cast<DevExpress.XtraGrid.Columns.GridColumn>()
                .ToDictionary(c => c.FieldName, c => c);

            // Verify business-friendly captions
            Assert.True(columnByField.TryGetValue("ShiftNumber",  out var colNum));
            Assert.Equal("Shift #", colNum.Caption);

            Assert.True(columnByField.TryGetValue("CashierName",  out var colCashier));
            Assert.Equal("Cashier", colCashier.Caption);

            Assert.True(columnByField.TryGetValue("OpenedAtUtc",  out var colOpened));
            Assert.Equal("Opened", colOpened.Caption);

            Assert.True(columnByField.TryGetValue("Status",       out var colStatus));
            Assert.Equal("Status", colStatus.Caption);

            Assert.True(columnByField.TryGetValue("CashVariance", out var colVariance));
            Assert.Equal("Variance", colVariance.Caption);
        }
    }
}
