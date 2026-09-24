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
        _gridView.OptionsSelection.MultiSelect = true;
        _gridView.SelectionChanged += async (s, e) => await TryRunAsync(UpdateActionButtonsStateAsync, "update the toolbar");
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

            var pickerItems = new List<CustomerPickerRow>();
            pickerItems.Add(new CustomerPickerRow(Guid.Empty, "-", "All Customers", string.Empty, string.Empty));
            pickerItems.AddRange(_allItems
                .Select(c => new CustomerPickerRow(c.CustomerId, c.Code, c.Name, c.MobileNumber, CurrencyDisplay.FormatPlain(c.OutstandingBalance))));

            var selectedVal = _txtSearch.EditValue;
            _txtSearch.Properties.DataSource = pickerItems;
            _txtSearch.EditValue = selectedVal ?? Guid.Empty;

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

        var selectedCount = _gridView.GetSelectedRows().Length;
        var focusedDto = GetFocusedCustomer();

        _newButton.Enabled = await _featurePolicy.CanUseFeatureAsync(userId, "customers.create");
        
        var canActivate = selectedCount > 0 && await _featurePolicy.CanUseFeatureAsync(userId, "customers.activate");
        var canDeactivate = selectedCount > 0 && await _featurePolicy.CanUseFeatureAsync(userId, "customers.deactivate");
        _btnToggleStatus.Enabled = canActivate || canDeactivate;
        _btnToggleStatus.Text = focusedDto != null && focusedDto.IsActive ? "Deactivate" : "Activate";

        var canEdit = await _featurePolicy.CanUseFeatureAsync(userId, "customers.edit");
        _btnSetDefault.Enabled = (selectedCount == 1) && focusedDto != null && focusedDto.IsActive && !focusedDto.IsDefault && canEdit;

        _btnLedger.Enabled = (selectedCount == 1) && focusedDto != null && await _featurePolicy.CanUseFeatureAsync(userId, "customers.viewledger");
        _btnReceivePayment.Enabled = (selectedCount == 1) && focusedDto != null && focusedDto.IsActive && await _featurePolicy.CanUseFeatureAsync(userId, "customers.payment");
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
        var statusFilter = _comboStatus.Text;

        var selectedId = GetFocusedCustomer()?.CustomerId;
        var focusedRowHandle = _gridView.FocusedRowHandle;
        var topRowIndex = _gridView.TopRowIndex;

        var filtered = _allItems.AsEnumerable();

        if (_txtSearch.EditValue is Guid selectedCustId && selectedCustId != Guid.Empty)
        {
            filtered = filtered.Where(x => x.CustomerId == selectedCustId);
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

        if (selectedId is { } id)
        {
            var newIndex = -1;
            for (int i = 0; i < _gridView.RowCount; i++)
            {
                if (_gridView.GetRow(i) is CustomerGridRow r && r.Dto.CustomerId == id)
                {
                    newIndex = i;
                    break;
                }
            }
            if (newIndex >= 0)
            {
                _gridView.FocusedRowHandle = newIndex;
            }
        }
        else if (focusedRowHandle >= 0 && focusedRowHandle < _gridView.RowCount)
        {
            _gridView.FocusedRowHandle = focusedRowHandle;
        }
        _gridView.TopRowIndex = topRowIndex;
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
        _lblTotalOutstanding.Text = $"Total Outstanding: {CurrencyDisplay.FormatPlain(totalOutstanding)}";
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
        _txtSearch.EditValue = Guid.Empty;
        _comboStatus.SelectedIndex = 0; // "All"
        await TryRunAsync(ApplyFiltersAsync, "clear the filters");
    }

    private async void NewButton_Click(object? sender, EventArgs e)
    {
        var customers = await _mediator.Send(new ListCustomersQuery());
        var nextNumber = 1;
        foreach (var c in customers)
        {
            var code = c.Code;
            if (code.StartsWith("C", StringComparison.OrdinalIgnoreCase) && 
                int.TryParse(code.Substring(1), out var num))
            {
                if (num >= nextNumber)
                {
                    nextNumber = num + 1;
                }
            }
        }
        var nextCode = $"C{nextNumber:D3}";

        using var form = new CustomerEditForm("New Customer", code: nextCode);
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
                form.NotesValue,
                form.ShopNoValue,
                form.Mobile2Value,
                form.PhoneValue,
                form.IsDefaultValue));

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

    private async void BtnSetDefault_Click(object? sender, EventArgs e)
    {
        if (GetFocusedCustomer() is not { } customer) return;

        if (!await CanUseFeatureAsync("edit"))
        {
            XtraMessageBox.Show(this, "You do not have permission to edit customers.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!customer.IsActive)
        {
            XtraMessageBox.Show(this, "Inactive customers cannot be set as default.", "Operation Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (customer.IsDefault)
        {
            return;
        }

        string confirmMsg = $"Set customer '{customer.Name}' ({customer.Code}) as the default customer for new POS orders?";
        if (XtraMessageBox.Show(this, confirmMsg, "Confirm Set Default", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            await _mediator.Send(new SetDefaultCustomerCommand(customer.CustomerId));
            await LogActivityAsync("Customer Default Changed", $"Customer: {customer.Name} ({customer.Code}) set as default POS customer");
            await RefreshAsync();
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
            $"Received payment of {CurrencyDisplay.FormatPlain(amount)} (Method: {paymentMethod}) " +
            $"for {customerName} ({customerCode}). Outstanding: {CurrencyDisplay.FormatPlain(outstandingAfter)}.";

        if (changeAmount > 0)
        {
            detail += $" Change handed back: {CurrencyDisplay.FormatPlain(changeAmount)}.";
        }

        return detail;
    }

    private bool _isLedgerDialogOpen;
    private async void BtnLedger_Click(object? sender, EventArgs e)
    {
        if (_isLedgerDialogOpen) return;
        if (GetFocusedCustomer() is not { } customer) return;

        if (!await CanUseFeatureAsync("viewledger"))
        {
            XtraMessageBox.Show(this, "You do not have permission to view the ledger.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _isLedgerDialogOpen = true;
        try
        {
            using var dialog = new CustomerLedgerDialog(_mediator, customer);
            dialog.ShowDialog(this);
            // Refresh balance in grid upon ledger dialog closing in case updates occurred
            await RefreshAsync();
        }
        finally
        {
            _isLedgerDialogOpen = false;
        }
    }

    private async void BtnToggleStatus_Click(object? sender, EventArgs e)
    {
        var selectedRows = _gridView.GetSelectedRows();
        var customers = selectedRows
            .Select(r => _gridView.GetRow(r) as CustomerGridRow)
            .Where(r => r != null)
            .Select(r => r!.Dto)
            .ToList();

        if (customers.Count == 0) return;

        bool targetActive = _btnToggleStatus.Text == "Activate";
        string targetStateText = targetActive ? "Activate" : "Deactivate";

        if (!await CanUseFeatureAsync(targetStateText.ToLower()))
        {
            XtraMessageBox.Show(this, $"You do not have permission to {targetStateText.ToLower()} customers.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string confirmMsg = customers.Count == 1
            ? $"Are you sure you want to {targetStateText.ToLower()} the customer '{customers[0].Name}' ({customers[0].Code})?"
            : $"Are you sure you want to {targetStateText.ToLower()} the {customers.Count} selected customers?";

        if (XtraMessageBox.Show(this, confirmMsg, $"Confirm {targetStateText}", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            foreach (var customer in customers)
            {
                await _mediator.Send(new SetCustomerStatusCommand(customer.CustomerId, targetActive));
                await LogActivityAsync($"Customer {targetStateText}d", $"Customer: {customer.Name} ({customer.Code})");
            }
            await RefreshAsync();
        }
    }

    private async void GridView_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e) =>
        await TryRunAsync(UpdateActionButtonsStateAsync, "update the toolbar");

    private async void GridView_RowCellClick(object sender, RowCellClickEventArgs e)
    {
        if (e.Clicks == 2 && _gridView.GetSelectedRows().Length == 1 && GetFocusedCustomer() is { } customer)
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
            isNew: false,
            dto.ShopNo,
            dto.Mobile2,
            dto.Phone,
            dto.IsDefault);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new UpdateCustomerCommand(
                dto.CustomerId,
                form.NameValue,
                form.MobileValue,
                form.AddressValue,
                form.EmailValue,
                form.CreditLimitValue,
                form.NotesValue,
                form.ShopNoValue,
                form.Mobile2Value,
                form.PhoneValue,
                form.IsDefaultValue));

            await LogActivityAsync("Customer Edited", $"Customer: {form.NameValue} ({dto.Code})");
            await RefreshAsync();
        }
    }


    private void GridView_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Column.FieldName is "OutstandingBalance" or "CreditLimit" && e.Value != null && e.Value != DBNull.Value)
        {
            try
            {
                var val = Convert.ToDecimal(e.Value);
                e.DisplayText = CurrencyDisplay.FormatPlain(val);
            }
            catch
            {
                // Fallback
            }
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
        _btnSetDefault.MinimumSize = LogicalToDeviceUnits(new Size(115, 32));
        _exportButton.MinimumSize = LogicalToDeviceUnits(new Size(95, 32));
        _refreshButton.MinimumSize = LogicalToDeviceUnits(new Size(80, 32));
        _newButton.MinimumSize = LogicalToDeviceUnits(new Size(130, 32));

        filterPanel.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(320));
        filterPanel.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(180));
        filterPanel.ColumnStyles[2] = new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(110));

        _gridView.RowHeight = LogicalToDeviceUnits(32);
        _gridView.ColumnPanelRowHeight = LogicalToDeviceUnits(40);
        _gridView.Appearance.HeaderPanel.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _gridView.Appearance.HeaderPanel.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
        _gridView.Appearance.HeaderPanel.Options.UseTextOptions = true;

        _gridView.Columns["Code"].MinWidth = LogicalToDeviceUnits(60);
        _gridView.Columns["Name"].MinWidth = LogicalToDeviceUnits(150);
        _gridView.Columns["MobileNumber"].MinWidth = LogicalToDeviceUnits(100);
        _gridView.Columns["Phone"].MinWidth = LogicalToDeviceUnits(100);
        _gridView.Columns["Email"].MinWidth = LogicalToDeviceUnits(150);
        _gridView.Columns["OutstandingBalance"].MinWidth = LogicalToDeviceUnits(100);
        _gridView.Columns["CreditLimit"].MinWidth = LogicalToDeviceUnits(100);
        _gridView.Columns["StatusText"].MinWidth = LogicalToDeviceUnits(80);
        _gridView.Columns["IsDefaultText"].MinWidth = LogicalToDeviceUnits(70);
        _gridView.Columns["LastTransactionText"].MinWidth = LogicalToDeviceUnits(140);
    }

    private sealed record CustomerPickerRow(Guid CustomerId, string CustomerCode, string Name, string Phone, string BalanceDisplay);

    // --- GRID VIEW ROW SHAPE ---
    private sealed class CustomerGridRow(CustomerDto dto)
    {
        public CustomerDto Dto { get; } = dto;
        public string Code => Dto.Code;
        public string Name => Dto.Name;
        public string MobileNumber => Dto.MobileNumber;
        public string Email => Dto.Email ?? "-";
        public string? ShopNo => Dto.ShopNo;
        public string? Mobile2 => Dto.Mobile2 ?? "-";
        public string? Phone => Dto.Phone ?? "-";
        public decimal OutstandingBalance => Dto.OutstandingBalance;
        public decimal CreditLimit => Dto.CreditLimit;
        public string StatusText => Dto.IsActive ? "Active" : "Inactive";
        public bool IsDefault => Dto.IsDefault;
        public string IsDefaultText => Dto.IsDefault ? "YES" : "";
        public string LastTransactionText => DateTimeDisplay.Format(Dto.LastTransactionDate);
    }
}
