using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Clovent.Restaurant.Application.Customers.Commands;
using Clovent.Restaurant.Application.Customers.Dtos;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Customers;

/// <summary>
/// Customers Management View: lists customer records, supports search,
/// CRUD, active/inactive toggles, receives payments, and views their ledger.
/// Visual Studio Designer compatible.
/// </summary>
public sealed partial class CustomersView : XtraUserControl
{
    private const string FeatureCode = "customers";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly ILogger<CustomersView> _logger;

    private List<CustomerDto> _allItems = [];
    private bool _isLoading;

    /// <summary>Builds the screen and starts its own DI scope.</summary>
    public CustomersView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();

        // This scope's RestaurantDbContext and IdentityDbContext are each a
        // single instance shared by everything this screen does, for as long
        // as the screen is open, and EF Core allows only one operation in
        // flight per context. Routing the mediator and the feature policy
        // through one shared gate is how every other Restaurant screen keeps
        // two overlapping async chains off the same context (defect D22) -
        // see SerializedMediator for the full reasoning.
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(
            _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _logger = _scope.ServiceProvider.GetRequiredService<ILogger<CustomersView>>();
        _currentSession = currentSession;

        InitializeComponent();
        ScaleLayoutAtRuntime();
    }

    /// <summary>Design-time-only constructor for Visual Studio Designer.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CustomersView()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _logger = null!;
        _currentSession = null!;

        InitializeComponent();
        ScaleLayoutAtRuntime();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope?.Dispose();
            _gate?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Re-reads the customer list from the application layer and rebinds the
    /// grid and the summary footer from that result.
    /// </summary>
    /// <remarks>
    /// Deliberately re-sends <see cref="ListCustomersQuery"/> every time
    /// rather than re-filtering <see cref="_allItems"/>: balances move
    /// underneath this screen constantly (every POS credit sale and every
    /// payment changes one), so anything short of a fresh read shows figures
    /// that were true when the screen opened (defect D5). The current search
    /// text and status filter are re-applied to the new result, so refreshing
    /// never silently widens what the operator is looking at.
    /// </remarks>
    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // The load is already gated, but two overlapping refreshes would still
        // queue up and rebind the grid twice for one user action; this keeps a
        // second trigger (Load racing an early Refresh click) from doing that.
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        Cursor = Cursors.WaitCursor;
        try
        {
            var items = await _mediator.Send(new ListCustomersQuery(), cancellationToken);
            _allItems = [.. items];
            await ApplyFiltersAsync();
        }
        finally
        {
            _isLoading = false;
            Cursor = Cursors.Default;
        }
    }

    /// <summary>
    /// Runs one user-triggered action, surfacing and logging anything it
    /// throws instead of leaving a dropped <see cref="Task"/> behind.
    /// </summary>
    /// <remarks>
    /// A discarded task that faults is not merely invisible: the finalizer
    /// re-raises it as <see cref="TaskScheduler.UnobservedTaskException"/>
    /// whenever the GC gets to it, which is how a failure here surfaced as an
    /// unexplained error dialog over an unrelated screen minutes later
    /// (defects D22/D24). Every async path off an event handler goes through
    /// here so there is nothing left to drop.
    /// </remarks>
    private Task TryRunAsync(Func<Task> action, string actionDescription) =>
        GuardedAction.RunAsync(this, _logger, action, actionDescription);

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task UpdateActionButtonsStateAsync()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        if (_currentSession.UserId is not { } userId) return;

        var focusedDto = GetFocusedCustomer();

        _newButton.Enabled = await _featurePolicy.CanUseFeatureAsync(userId, "customers.create");
        
        var canActivate = focusedDto != null && !focusedDto.IsActive && await _featurePolicy.CanUseFeatureAsync(userId, "customers.activate");
        var canDeactivate = focusedDto != null && focusedDto.IsActive && await _featurePolicy.CanUseFeatureAsync(userId, "customers.deactivate");
        _btnToggleStatus.Enabled = canActivate || canDeactivate;
        _btnToggleStatus.Text = focusedDto != null && focusedDto.IsActive ? "Deactivate" : "Activate";

        _btnLedger.Enabled = focusedDto != null && await _featurePolicy.CanUseFeatureAsync(userId, "customers.viewledger");
        _btnReceivePayment.Enabled = focusedDto != null && focusedDto.IsActive && await _featurePolicy.CanUseFeatureAsync(userId, "customers.payment");
    }

    private CustomerDto? GetFocusedCustomer()
    {
        if (_gridView.GetFocusedRow() is CustomerGridRow row)
        {
            return row.Dto;
        }
        return null;
    }

    private async Task ApplyFiltersAsync()
    {
        var searchText = _txtSearch.Text.Trim();
        var statusFilter = _comboStatus.Text;

        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(x =>
                x.Code.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                x.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                x.MobileNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (x.Email != null && x.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        if (statusFilter == "Active")
        {
            filtered = filtered.Where(x => x.IsActive);
        }
        else if (statusFilter == "Inactive")
        {
            filtered = filtered.Where(x => !x.IsActive);
        }

        var list = filtered.ToList();
        _gridControl.DataSource = list.Select(x => new CustomerGridRow(x)).ToList();

        UpdateSummaryMetrics(list);
        await UpdateActionButtonsStateAsync();
    }

    private void UpdateSummaryMetrics(List<CustomerDto> visibleItems)
    {
        var total = visibleItems.Count;
        var active = visibleItems.Count(x => x.IsActive);
        var withBalance = visibleItems.Count(x => x.OutstandingBalance > 0);
        var totalOutstanding = visibleItems.Sum(x => x.OutstandingBalance);

        _lblTotalCustomers.Text = $"Total Customers: {total}";
        _lblActiveCustomers.Text = $"Active: {active}";
        _lblWithBalance.Text = $"With Balance: {withBalance}";
        _lblTotalOutstanding.Text = $"Total Outstanding: {CurrencyDisplay.Format(totalOutstanding)}";
    }

    private async Task LogActivityAsync(string action, string? details = null)
    {
        try
        {
            await _mediator.Send(new RecordActivityCommand(action, details, _currentSession.DisplayName ?? "Unknown", Environment.MachineName));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Swallowed per auditing guidelines
        }
    }

    // --- EVENT HANDLERS ---

    private async void CustomersView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        AppearanceManager.Apply(this, "Restaurant", nameof(CustomersView));
        await RefreshAsync();
    }

    private async void TxtSearch_EditValueChanged(object? sender, EventArgs e) =>
        await TryRunAsync(ApplyFiltersAsync, "apply the search filter");

    private async void ComboStatus_SelectedIndexChanged(object? sender, EventArgs e) =>
        await TryRunAsync(ApplyFiltersAsync, "apply the status filter");

    private async void BtnClearFilters_Click(object? sender, EventArgs e)
    {
        _txtSearch.Text = string.Empty;
        _comboStatus.SelectedIndex = 0; // "All"
        await TryRunAsync(ApplyFiltersAsync, "clear the filters");
    }

    private async void NewButton_Click(object? sender, EventArgs e)
    {
        using var form = new CustomerEditForm("New Customer");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateCustomerCommand(
                form.CodeValue,
                form.NameValue,
                form.MobileValue,
                form.AddressValue,
                form.EmailValue,
                form.OpeningBalanceValue,
                form.CreditLimitValue,
                form.NotesValue));

            await LogActivityAsync("Customer Created", $"Customer: {form.NameValue} ({form.CodeValue})");
            await RefreshAsync();
        }
    }

    private async void RefreshButton_Click(object? sender, EventArgs e) => await RefreshAsync();

    private void ExportButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "Customers.csv" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _gridView.ExportToCsv(dialog.FileName);
            XtraMessageBox.Show(this, "Customers exported successfully.", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void BtnReceivePayment_Click(object? sender, EventArgs e)
    {
        if (GetFocusedCustomer() is not { } customer) return;

        if (!await CanUseFeatureAsync("payment"))
        {
            XtraMessageBox.Show(this, "You do not have permission to receive payments.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // The methods a payment may be recorded against are the ones the owner
        // configured, not a list hardcoded into this dialog: a hardcoded list
        // drifts from the configured one and silently records payments against
        // methods that do not exist (defect D9).
        var paymentMethods = await _mediator.Send(new ListPaymentMethodsQuery());
        var activeMethodNames = paymentMethods
            .Where(m => m.Status == "Active")
            .Select(m => m.Name)
            .ToList();

        if (activeMethodNames.Count == 0)
        {
            XtraMessageBox.Show(
                this,
                "No active payment methods are configured. Add one under Payment Methods before receiving a payment.",
                "No Payment Methods",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var form = new CustomerPaymentForm(customer, activeMethodNames);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            var res = await _mediator.Send(new RecordCustomerPaymentCommand(
                customer.CustomerId,
                form.Amount,
                form.PaymentMethod,
                form.Reference,
                form.Notes));

            var detailMsg = ComposePaymentActivityDetail(
                form.Amount,
                form.PaymentMethod,
                customer.Name,
                customer.Code,
                res.OutstandingAfter,
                res.ChangeAmount);

            await LogActivityAsync("Customer Payment", detailMsg);
            XtraMessageBox.Show(this, detailMsg, "Payment Recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshAsync();
        }
    }

    /// <summary>
    /// Builds the one sentence that is both shown to the operator and written
    /// to the activity log after a customer payment.
    /// </summary>
    /// <remarks>
    /// Pulled out of the click handler so its wording is reachable by a test:
    /// this text is persisted to the audit log, where it read "Outstanding
    /// outstanding: $317.50" on every payment ever recorded (defect D11).
    /// </remarks>
    public static string ComposePaymentActivityDetail(
        decimal amount,
        string paymentMethod,
        string customerName,
        string customerCode,
        decimal outstandingAfter,
        decimal changeAmount)
    {
        var detail =
            $"Received payment of {CurrencyDisplay.Format(amount)} (Method: {paymentMethod}) " +
            $"for {customerName} ({customerCode}). Outstanding: {CurrencyDisplay.Format(outstandingAfter)}.";

        if (changeAmount > 0)
        {
            detail += $" Change handed back: {CurrencyDisplay.Format(changeAmount)}.";
        }

        return detail;
    }

    private async void BtnLedger_Click(object? sender, EventArgs e)
    {
        if (GetFocusedCustomer() is not { } customer) return;

        if (!await CanUseFeatureAsync("viewledger"))
        {
            XtraMessageBox.Show(this, "You do not have permission to view the ledger.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var dialog = new CustomerLedgerDialog(_mediator, customer);
        dialog.ShowDialog(this);
        // Refresh balance in grid upon ledger dialog closing in case updates occurred
        await RefreshAsync();
    }

    private async void BtnToggleStatus_Click(object? sender, EventArgs e)
    {
        if (GetFocusedCustomer() is not { } customer) return;

        string targetStateText = customer.IsActive ? "Deactivate" : "Activate";
        if (!await CanUseFeatureAsync(targetStateText.ToLower()))
        {
            XtraMessageBox.Show(this, $"You do not have permission to {targetStateText.ToLower()} customers.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string confirmMsg = $"Are you sure you want to {targetStateText.ToLower()} the customer '{customer.Name}' ({customer.Code})?";

        if (XtraMessageBox.Show(this, confirmMsg, $"Confirm {targetStateText}", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            await _mediator.Send(new SetCustomerStatusCommand(customer.CustomerId, !customer.IsActive));
            await LogActivityAsync($"Customer {targetStateText}d", $"Customer: {customer.Name} ({customer.Code})");
            await RefreshAsync();
        }
    }

    private async void GridView_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e) =>
        await TryRunAsync(UpdateActionButtonsStateAsync, "update the toolbar");

    private async void GridView_RowCellClick(object sender, RowCellClickEventArgs e)
    {
        if (e.Clicks == 2 && GetFocusedCustomer() is { } customer)
        {
            if (await CanUseFeatureAsync("edit"))
            {
                await EditAsync(customer);
            }
        }
    }

    private async Task EditAsync(CustomerDto dto)
    {
        using var form = new CustomerEditForm(
            "Edit Customer",
            dto.Code,
            dto.Name,
            dto.MobileNumber,
            dto.Address,
            dto.Email,
            dto.OpeningBalance,
            dto.CreditLimit,
            dto.Notes,
            isNew: false);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new UpdateCustomerCommand(
                dto.CustomerId,
                form.NameValue,
                form.MobileValue,
                form.AddressValue,
                form.EmailValue,
                form.CreditLimitValue,
                form.NotesValue));

            await LogActivityAsync("Customer Edited", $"Customer: {form.NameValue} ({dto.Code})");
            await RefreshAsync();
        }
    }

    private void GridView_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Column.FieldName is "OutstandingBalance" or "CreditLimit" && e.Value is decimal val)
        {
            e.DisplayText = CurrencyDisplay.Format(val);
        }
    }

    private void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        root.RowStyles[0] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(55));
        root.RowStyles[1] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(45));
        root.RowStyles[3] = new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(40));

        topPanel.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(180));

        _btnLedger.MinimumSize = LogicalToDeviceUnits(new Size(110, 32));
        _btnReceivePayment.MinimumSize = LogicalToDeviceUnits(new Size(130, 32));
        _btnToggleStatus.MinimumSize = LogicalToDeviceUnits(new Size(100, 32));
        _exportButton.MinimumSize = LogicalToDeviceUnits(new Size(95, 32));
        _refreshButton.MinimumSize = LogicalToDeviceUnits(new Size(80, 32));
        _newButton.MinimumSize = LogicalToDeviceUnits(new Size(130, 32));

        filterPanel.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(320));
        filterPanel.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(180));
        filterPanel.ColumnStyles[2] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(110));

        _gridView.RowHeight = LogicalToDeviceUnits(32);
    }

    // --- GRID VIEW ROW SHAPE ---
    private sealed class CustomerGridRow(CustomerDto dto)
    {
        public CustomerDto Dto { get; } = dto;
        public string Code => Dto.Code;
        public string Name => Dto.Name;
        public string MobileNumber => Dto.MobileNumber;
        public string Email => Dto.Email ?? "-";
        public decimal OutstandingBalance => Dto.OutstandingBalance;
        public decimal CreditLimit => Dto.CreditLimit;
        public string StatusText => Dto.IsActive ? "Active" : "Inactive";
        public string LastTransactionText => Dto.LastTransactionDate?.ToLocalTime().ToString("g") ?? "-";
    }
}
