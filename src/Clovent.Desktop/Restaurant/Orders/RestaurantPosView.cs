using Clovent.Catalog.Application.Barcodes.Queries;
using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Inventory.WarehouseStocks;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Restaurant.Shared;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.Warehouses.Queries;
using Clovent.Restaurant.Application;
using Clovent.Restaurant.Application.Discounts.Commands;
using Clovent.Restaurant.Application.Discounts.Queries;
using Clovent.Restaurant.Application.KitchenTickets.Commands;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.ServiceCharges.Commands;
using Clovent.Restaurant.Application.ServiceCharges.Queries;
using Clovent.Restaurant.Application.Tables.Queries;
using Clovent.Restaurant.Orders;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// The Restaurant POS Core screen: pick a table (Dine In) or start a Take
/// Away order, add items by barcode or from the product grid, manage
/// quantities/notes/discounts/service charges, and drive an order through
/// its lifecycle (Hold/Resume/Void/Cancel/Reopen/Send to Kitchen/Complete),
/// including Table Transfer/Merge/Split Bill. Payment is handled by the
/// dedicated <see cref="PaymentForm"/>, launched from here. Feature-gated
/// per <c>pos.{operation}</c> for every state-changing action.
/// </summary>
public sealed class RestaurantPosView : XtraUserControl
{
    private const string FeatureCode = "pos";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private readonly EntityPicker _warehousePicker = new("Warehouse:");
    private readonly EntityPicker _tablePicker = new("Table:");
    private readonly LabelControl _orderStatusLabel = new() { Text = "No order selected." };
    private readonly SimpleButton _newDineInButton = new() { Text = "New Dine-In", Height = 40, Width = 130 };
    private readonly SimpleButton _newTakeAwayButton = new() { Text = "New Take Away", Height = 40, Width = 130 };

    private readonly SimpleButton _holdButton = new() { Text = "Hold", Height = 40 };
    private readonly SimpleButton _resumeButton = new() { Text = "Resume", Height = 40 };
    private readonly SimpleButton _sendToKitchenButton = new() { Text = "Send to Kitchen", Height = 40 };
    private readonly SimpleButton _payButton = new() { Text = "Pay...", Height = 40 };
    private readonly SimpleButton _completeButton = new() { Text = "Complete Order", Height = 40 };
    private readonly SimpleButton _voidOrderButton = new() { Text = "Void Order", Height = 40 };
    private readonly SimpleButton _cancelOrderButton = new() { Text = "Cancel Order", Height = 40 };
    private readonly SimpleButton _reopenButton = new() { Text = "Reopen Order", Height = 40 };
    private readonly SimpleButton _transferTableButton = new() { Text = "Transfer Table", Height = 36 };
    private readonly SimpleButton _mergeTablesButton = new() { Text = "Merge Tables", Height = 36 };
    private readonly SimpleButton _splitBillButton = new() { Text = "Split Bill", Height = 36 };
    private readonly SimpleButton _orderNotesButton = new() { Text = "Order Notes", Height = 36 };
    private readonly SimpleButton _customerNotesButton = new() { Text = "Customer Notes", Height = 36 };
    private readonly SimpleButton _addDiscountButton = new() { Text = "Add Discount", Height = 36 };
    private readonly SimpleButton _removeDiscountButton = new() { Text = "Remove Discount", Height = 36 };
    private readonly SimpleButton _addServiceChargeButton = new() { Text = "Add Service Charge", Height = 36 };
    private readonly SimpleButton _removeServiceChargeButton = new() { Text = "Remove Service Charge", Height = 36 };

    private readonly LabelControl _subtotalLabel = new();
    private readonly LabelControl _taxLabel = new();
    private readonly LabelControl _discountLabel = new();
    private readonly LabelControl _serviceChargeLabel = new();
    private readonly LabelControl _grandTotalLabel = new();
    private readonly LabelControl _paidLabel = new();
    private readonly LabelControl _balanceLabel = new();

    private readonly TextEdit _barcodeEdit = new();
    private readonly SimpleButton _addByBarcodeButton = new() { Text = "Add", Height = 30 };
    private readonly SpinEdit _addQuantityEdit = new() { Properties = { MinValue = 0.01m, MaxValue = 1000 }, Value = 1 };
    private readonly TextEdit _productSearchEdit = new();
    private readonly FlowLayoutPanel _categoryButtonsPanel = new() { Dock = DockStyle.Top, Height = 32, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
    private readonly GridControl _productGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _productGridView = new();

    private readonly GridControl _lineGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _lineGridView = new();
    private readonly SimpleButton _editQuantityButton = new() { Text = "Edit Qty", Height = 32 };
    private readonly SimpleButton _editLineNotesButton = new() { Text = "Item Notes", Height = 32 };
    private readonly SimpleButton _voidLineButton = new() { Text = "Void Line", Height = 32 };
    private readonly SimpleButton _removeLineButton = new() { Text = "Remove Line", Height = 32 };
    private readonly SimpleButton _refreshButton = new() { Text = "Refresh", Height = 32 };

    private OrderDto? _currentOrder;
    private readonly Dictionary<Guid, ProductVariantDto> _variantsById = [];
    private Dictionary<string, bool> _permissions = [];
    private List<ProductVariantDto> _activeVariants = [];
    private Guid? _selectedCategoryId;
    private readonly SimpleButton _allCategoriesButton = new() { Text = "All", Height = 26 };

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public RestaurantPosView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _orderStatusLabel.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
        _grandTotalLabel.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);

        BuildProductGrid();
        BuildLineGrid();
        BuildLayout();
        WireEvents();

        Load += async (_, _) => await LoadAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildProductGrid()
    {
        _productGrid.MainView = _productGridView;
        _productGrid.ViewCollection.Add(_productGridView);
        _productGridView.OptionsBehavior.Editable = false;
        _productGridView.OptionsSelection.MultiSelect = false;
        _productGridView.OptionsView.ShowGroupPanel = false;
        _productGridView.Columns.AddVisible(nameof(ProductVariantDto.Sku), "SKU");
        _productGridView.Columns.AddVisible(nameof(ProductVariantDto.Name), "Name");
    }

    private void BuildLineGrid()
    {
        _lineGrid.MainView = _lineGridView;
        _lineGrid.ViewCollection.Add(_lineGridView);
        _lineGridView.OptionsBehavior.Editable = false;
        _lineGridView.OptionsSelection.MultiSelect = false;
        _lineGridView.OptionsView.ShowGroupPanel = false;
        _lineGridView.Columns.AddVisible(nameof(OrderLineRow.Sku), "SKU");
        _lineGridView.Columns.AddVisible(nameof(OrderLineRow.Name), "Name");
        _lineGridView.Columns.AddVisible(nameof(OrderLineRow.Quantity), "Qty");
        _lineGridView.Columns.AddVisible(nameof(OrderLineRow.UnitPrice), "Unit Price");
        _lineGridView.Columns.AddVisible(nameof(OrderLineRow.LineTotal), "Total");
        _lineGridView.Columns.AddVisible(nameof(OrderLineRow.Notes), "Notes");
        _lineGridView.Columns.AddVisible(nameof(OrderLineRow.IsVoided), "Voided");
    }

    private void BuildLayout()
    {
        var centerToolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        centerToolbar.Controls.Add(_editQuantityButton);
        centerToolbar.Controls.Add(_editLineNotesButton);
        centerToolbar.Controls.Add(_voidLineButton);
        centerToolbar.Controls.Add(_removeLineButton);
        centerToolbar.Controls.Add(_refreshButton);

        var centerPanel = new PanelControl { Dock = DockStyle.Fill };
        centerPanel.Controls.Add(_lineGrid);
        centerPanel.Controls.Add(centerToolbar);

        var rightPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 300,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8),
        };
        foreach (var control in new Control[]
        {
            _holdButton, _resumeButton, _sendToKitchenButton, _payButton, _completeButton,
            _voidOrderButton, _cancelOrderButton, _reopenButton,
            _transferTableButton, _mergeTablesButton, _splitBillButton,
            _orderNotesButton, _customerNotesButton,
            _addDiscountButton, _removeDiscountButton, _addServiceChargeButton, _removeServiceChargeButton,
        })
        {
            control.Width = 260;
            rightPanel.Controls.Add(control);
        }
        rightPanel.Controls.Add(new SeparatorControl { Width = 260 });
        rightPanel.Controls.Add(_subtotalLabel);
        rightPanel.Controls.Add(_taxLabel);
        rightPanel.Controls.Add(_discountLabel);
        rightPanel.Controls.Add(_serviceChargeLabel);
        rightPanel.Controls.Add(_grandTotalLabel);
        rightPanel.Controls.Add(_paidLabel);
        rightPanel.Controls.Add(_balanceLabel);

        var leftToolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 36, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        _barcodeEdit.Width = 120;
        _barcodeEdit.Properties.NullValuePrompt = "Scan or type barcode/SKU";
        _addQuantityEdit.Width = 60;
        leftToolbar.Controls.Add(_barcodeEdit);
        leftToolbar.Controls.Add(_addQuantityEdit);
        leftToolbar.Controls.Add(_addByBarcodeButton);

        var searchPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        _productSearchEdit.Width = 280;
        _productSearchEdit.Properties.NullValuePrompt = "Search products...";
        searchPanel.Controls.Add(_productSearchEdit);

        var leftPanel = new PanelControl { Dock = DockStyle.Left, Width = 320 };
        leftPanel.Controls.Add(_productGrid);
        leftPanel.Controls.Add(_categoryButtonsPanel);
        leftPanel.Controls.Add(searchPanel);
        leftPanel.Controls.Add(leftToolbar);

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(4) };
        topPanel.Controls.Add(_warehousePicker);
        topPanel.Controls.Add(_tablePicker);
        topPanel.Controls.Add(_newDineInButton);
        topPanel.Controls.Add(_newTakeAwayButton);
        topPanel.Controls.Add(_orderStatusLabel);

        Controls.Add(centerPanel);
        Controls.Add(rightPanel);
        Controls.Add(leftPanel);
        Controls.Add(topPanel);
    }

    private void WireEvents()
    {
        _tablePicker.SelectionChanged += async (_, _) => await OnTableSelectedAsync();
        _newDineInButton.Click += async (_, _) => await NewDineInAsync();
        _newTakeAwayButton.Click += async (_, _) => await NewTakeAwayAsync();

        _holdButton.Click += async (_, _) => await RunOrderActionAsync(new HoldOrderCommand(_currentOrder!.OrderId));
        _resumeButton.Click += async (_, _) => await RunOrderActionAsync(new ResumeOrderCommand(_currentOrder!.OrderId));
        _sendToKitchenButton.Click += async (_, _) => await SendToKitchenAsync();
        _payButton.Click += async (_, _) => await PayAsync();
        _completeButton.Click += async (_, _) => await CompleteAsync();
        _voidOrderButton.Click += async (_, _) => await VoidOrderAsync();
        _cancelOrderButton.Click += async (_, _) => await CancelOrderAsync();
        _reopenButton.Click += async (_, _) => await RunOrderActionAsync(new ReopenOrderCommand(_currentOrder!.OrderId));
        _transferTableButton.Click += async (_, _) => await TransferTableAsync();
        _mergeTablesButton.Click += async (_, _) => await MergeTablesAsync();
        _splitBillButton.Click += async (_, _) => await SplitBillAsync();
        _orderNotesButton.Click += async (_, _) => await SetOrderNotesAsync();
        _customerNotesButton.Click += async (_, _) => await SetCustomerNotesAsync();
        _addDiscountButton.Click += async (_, _) => await AddDiscountAsync();
        _removeDiscountButton.Click += async (_, _) => await RemoveDiscountAsync();
        _addServiceChargeButton.Click += async (_, _) => await AddServiceChargeAsync();
        _removeServiceChargeButton.Click += async (_, _) => await RemoveServiceChargeAsync();

        _productSearchEdit.EditValueChanged += (_, _) => ApplyProductFilter();
        _allCategoriesButton.Click += (_, _) => SelectCategory(null);

        _addByBarcodeButton.Click += async (_, _) => await AddByBarcodeAsync();
        _barcodeEdit.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                await AddByBarcodeAsync();
            }
        };
        _productGridView.DoubleClick += async (_, _) => await AddSelectedVariantAsync();

        _editQuantityButton.Click += async (_, _) => await EditQuantityAsync();
        _editLineNotesButton.Click += async (_, _) => await EditLineNotesAsync();
        _voidLineButton.Click += async (_, _) => await VoidLineAsync();
        _removeLineButton.Click += async (_, _) => await RemoveLineAsync();
        _refreshButton.Click += async (_, _) => await RefreshOrderAsync();
    }

    private async Task LoadAsync()
    {
        var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
        _warehousePicker.LoadItems([.. warehouses.Select(w => (w.WarehouseId, w.Name))]);

        var variants = await _mediator.Send(new ListProductVariantsQuery());
        foreach (var variant in variants)
        {
            _variantsById[variant.ProductVariantId] = variant;
        }
        _activeVariants = [.. variants.Where(v => v.Status == "Active")];

        var categories = await _mediator.Send(new ListProductCategoriesQuery());
        BuildCategoryButtons(categories.Where(c => c.Status == "Active").ToList());
        ApplyProductFilter();

        await ReloadTablesAsync();
        await RefreshOrderAsync();
    }

    private void BuildCategoryButtons(IReadOnlyList<ProductCategoryDto> categories)
    {
        _categoryButtonsPanel.Controls.Clear();
        _categoryButtonsPanel.Controls.Add(_allCategoriesButton);

        foreach (var category in categories)
        {
            var button = new SimpleButton { Text = category.Name, Height = 26, Tag = category.ProductCategoryId };
            button.Click += (_, _) => SelectCategory(category.ProductCategoryId);
            _categoryButtonsPanel.Controls.Add(button);
        }
    }

    private void SelectCategory(Guid? categoryId)
    {
        _selectedCategoryId = categoryId;
        ApplyProductFilter();
    }

    private void ApplyProductFilter()
    {
        var searchText = _productSearchEdit.Text.Trim();

        var filtered = _activeVariants.AsEnumerable();
        if (_selectedCategoryId is { } categoryId)
        {
            filtered = filtered.Where(v => v.ProductCategoryId == categoryId);
        }

        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(v =>
                v.Sku.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                v.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        _productGrid.DataSource = filtered.ToList();
    }

    private async Task ReloadTablesAsync()
    {
        var selectedTableId = _tablePicker.SelectedId;
        var tables = await _mediator.Send(new ListAllTablesQuery());
        _tablePicker.LoadItems([.. tables.Select(t => (t.TableId, $"{t.Code} ({t.OccupancyStatus})"))]);
        _tablePicker.SelectId(selectedTableId);
    }

    private async Task OnTableSelectedAsync()
    {
        if (_tablePicker.SelectedId is not { } tableId)
        {
            _currentOrder = null;
            await RefreshOrderAsync();
            return;
        }

        _currentOrder = await _mediator.Send(new GetOpenOrHeldOrderByTableQuery(tableId));
        await RefreshOrderAsync();
    }

    private async Task NewDineInAsync()
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            XtraMessageBox.Show(this, "Select a warehouse first.", "No Warehouse Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_tablePicker.SelectedId is not { } tableId)
        {
            XtraMessageBox.Show(this, "Select a table first.", "No Table Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _currentOrder = await _mediator.Send(new CreateOrderCommand(OrderType.DineIn, warehouseId, tableId));
        await RefreshOrderAsync();
    }

    private async Task NewTakeAwayAsync()
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            XtraMessageBox.Show(this, "Select a warehouse first.", "No Warehouse Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _currentOrder = await _mediator.Send(new CreateOrderCommand(OrderType.TakeAway, warehouseId));
        await RefreshOrderAsync();
    }

    private async Task RunOrderActionAsync(IRequest<OrderDto> command)
    {
        _currentOrder = await _mediator.Send(command);
        await RefreshOrderAsync();
    }

    private async Task VoidOrderAsync()
    {
        using var form = new TextPromptForm("Void Order", "Reason:", required: true);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new VoidOrderCommand(_currentOrder!.OrderId, form.Value!));
            await RefreshOrderAsync();
        }
    }

    private async Task CancelOrderAsync()
    {
        using var form = new TextPromptForm("Cancel Order", "Reason:", required: true);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new CancelOrderCommand(_currentOrder!.OrderId, form.Value!));
            await RefreshOrderAsync();
        }
    }

    private async Task SendToKitchenAsync()
    {
        var ticket = await _mediator.Send(new SendOrderToKitchenCommand(_currentOrder!.OrderId));
        XtraMessageBox.Show(this, $"Kitchen ticket created ({ticket.Status}).", "Sent to Kitchen", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task CompleteAsync()
    {
        await _mediator.Send(new CompleteOrderCommand(_currentOrder!.OrderId));
        _currentOrder = null;
        _tablePicker.SelectId(null);
        await ReloadTablesAsync();
        await RefreshOrderAsync();
    }

    private async Task PayAsync()
    {
        using var form = new PaymentForm(_mediator, _currentOrder!.OrderId);
        form.ShowDialog(this);
        await RefreshOrderAsync();
    }

    private async Task TransferTableAsync()
    {
        var tables = await _mediator.Send(new ListAllTablesQuery());
        var options = tables
            .Where(t => t.OccupancyStatus == "Available" && t.TableId != _currentOrder!.TableId)
            .Select(t => (t.TableId, t.Code))
            .ToList();

        if (options.Count == 0)
        {
            XtraMessageBox.Show(this, "No available tables to transfer to.", "No Tables Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new TableTransferDialog(options);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new TransferOrderTableCommand(_currentOrder!.OrderId, form.NewTableId!.Value));
            _tablePicker.SelectId(_currentOrder.TableId);
            await RefreshOrderAsync();
        }
    }

    private async Task MergeTablesAsync()
    {
        var tables = await _mediator.Send(new ListAllTablesQuery());
        var options = tables.Select(t => (t.TableId, t.Code)).ToList();

        using var form = new MergeTablesDialog(options, _currentOrder!.TableId);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new MergeTablesCommand(form.SourceTableId!.Value, form.TargetTableId!.Value));
            _tablePicker.SelectId(_currentOrder.TableId);
            await RefreshOrderAsync();
        }
    }

    private async Task SplitBillAsync()
    {
        var lines = await _mediator.Send(new ListOrderLinesByOrderQuery(_currentOrder!.OrderId));
        var activeLines = lines.Where(l => !l.IsVoided).ToList();
        if (activeLines.Count == 0)
        {
            XtraMessageBox.Show(this, "There are no active lines to split.", "Nothing to Split", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var lineOptions = activeLines.Select(l => (l.OrderLineId, $"{ResolveVariantDisplay(l.ProductVariantId)} x{l.Quantity:N2}")).ToList();

        var tables = await _mediator.Send(new ListAllTablesQuery());
        var tableOptions = tables.Where(t => t.OccupancyStatus == "Available").Select(t => (t.TableId, t.Code)).ToList();
        if (tableOptions.Count == 0)
        {
            XtraMessageBox.Show(this, "No available tables for the split.", "No Tables Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new BillSplitDialog(lineOptions, tableOptions);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new SplitOrderCommand(_currentOrder!.OrderId, form.SelectedOrderLineIds, form.TargetTableId!.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task SetOrderNotesAsync()
    {
        using var form = new TextPromptForm("Order Notes", "Notes:", _currentOrder!.Notes);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new SetOrderNotesCommand(_currentOrder!.OrderId, form.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task SetCustomerNotesAsync()
    {
        using var form = new TextPromptForm("Customer Notes", "Notes:", _currentOrder!.CustomerNotes);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _currentOrder = await _mediator.Send(new SetOrderCustomerNotesCommand(_currentOrder!.OrderId, form.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task AddDiscountAsync()
    {
        using var form = new DiscountDialog();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ApplyDiscountToOrderCommand(_currentOrder!.OrderId, form.DiscountType, form.Value, form.Reason));
            await RefreshOrderAsync();
        }
    }

    private async Task RemoveDiscountAsync()
    {
        var discounts = await _mediator.Send(new ListDiscountsByOrderQuery(_currentOrder!.OrderId));
        if (discounts.Count == 0)
        {
            XtraMessageBox.Show(this, "There are no discounts to remove.", "No Discounts", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var options = discounts.Select(d => (d.DiscountId, $"{d.DiscountType} {d.Value:N2} - {d.Reason}")).ToList();
        using var form = new SelectionPromptForm("Remove Discount", "Discount:", options);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RemoveDiscountFromOrderCommand(_currentOrder!.OrderId, form.SelectedId!.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task AddServiceChargeAsync()
    {
        using var form = new ServiceChargeDialog();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ApplyServiceChargeToOrderCommand(_currentOrder!.OrderId, form.ServiceChargeType, form.Value, form.Reason));
            await RefreshOrderAsync();
        }
    }

    private async Task RemoveServiceChargeAsync()
    {
        var charges = await _mediator.Send(new ListServiceChargesByOrderQuery(_currentOrder!.OrderId));
        if (charges.Count == 0)
        {
            XtraMessageBox.Show(this, "There are no service charges to remove.", "No Service Charges", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var options = charges.Select(c => (c.ServiceChargeId, $"{c.ServiceChargeType} {c.Value:N2} - {c.Reason}")).ToList();
        using var form = new SelectionPromptForm("Remove Service Charge", "Service Charge:", options);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RemoveServiceChargeFromOrderCommand(_currentOrder!.OrderId, form.SelectedId!.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task AddByBarcodeAsync()
    {
        var value = _barcodeEdit.Text.Trim();
        if (string.IsNullOrEmpty(value) || _currentOrder is null)
        {
            return;
        }

        Guid variantId;
        try
        {
            var barcode = await _mediator.Send(new GetBarcodeByValueQuery(value));
            variantId = barcode.ProductVariantId;
        }
        catch (NotFoundException)
        {
            var bySku = _variantsById.Values.FirstOrDefault(v => v.Sku.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (bySku is null)
            {
                XtraMessageBox.Show(this, $"No product found for '{value}'.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            variantId = bySku.ProductVariantId;
        }

        await _mediator.Send(new AddOrderLineCommand(_currentOrder.OrderId, variantId, _addQuantityEdit.Value));
        _barcodeEdit.Text = string.Empty;
        await RefreshOrderAsync();
    }

    private async Task AddSelectedVariantAsync()
    {
        if (_currentOrder is null || _productGridView.GetFocusedRow() is not ProductVariantDto variant)
        {
            return;
        }

        await _mediator.Send(new AddOrderLineCommand(_currentOrder.OrderId, variant.ProductVariantId, _addQuantityEdit.Value));
        await RefreshOrderAsync();
    }

    private async Task EditQuantityAsync()
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        using var form = new QuantityPromptForm("Edit Quantity", "Quantity:");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new SetOrderLineQuantityCommand(row.OrderLineId, form.Quantity));
            await RefreshOrderAsync();
        }
    }

    private async Task EditLineNotesAsync()
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        using var form = new TextPromptForm("Item Notes", "Notes:", row.Notes);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new SetOrderLineNotesCommand(row.OrderLineId, form.Value));
            await RefreshOrderAsync();
        }
    }

    private async Task VoidLineAsync()
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        await _mediator.Send(new VoidOrderLineCommand(row.OrderLineId));
        await RefreshOrderAsync();
    }

    private async Task RemoveLineAsync()
    {
        if (_lineGridView.GetFocusedRow() is not OrderLineRow row)
        {
            return;
        }

        await _mediator.Send(new RemoveOrderLineCommand(_currentOrder!.OrderId, row.OrderLineId));
        await RefreshOrderAsync();
    }

    private async Task RefreshOrderAsync()
    {
        await UpdatePermissionsAsync();

        if (_currentOrder is null)
        {
            _lineGrid.DataSource = null;
            SetTotals(null);
            _orderStatusLabel.Text = "No order selected.";
            UpdateButtonStates();
            return;
        }

        _currentOrder = await _mediator.Send(new GetOrderByIdQuery(_currentOrder.OrderId));

        var lines = await _mediator.Send(new ListOrderLinesByOrderQuery(_currentOrder.OrderId));
        _lineGrid.DataSource = lines.Select(l => new OrderLineRow(
            l.OrderLineId,
            ResolveVariantSku(l.ProductVariantId),
            ResolveVariantName(l.ProductVariantId),
            l.Quantity,
            l.UnitPrice,
            l.LineTotal,
            l.Notes ?? string.Empty,
            l.IsVoided)).ToList();

        var totals = await _mediator.Send(new GetOrderSummaryQuery(_currentOrder.OrderId));
        SetTotals(totals);

        _orderStatusLabel.Text = $"{_currentOrder.OrderNumber} - {_currentOrder.OrderType} - {_currentOrder.Status}";

        await ReloadTablesAsync();
        UpdateButtonStates();
    }

    private void SetTotals(OrderTotals? totals)
    {
        _subtotalLabel.Text = $"Subtotal: {totals?.Subtotal ?? 0m:N2}";
        _taxLabel.Text = $"Tax: {totals?.TaxTotal ?? 0m:N2}";
        _discountLabel.Text = $"Discount: {totals?.DiscountTotal ?? 0m:N2}";
        _serviceChargeLabel.Text = $"Service Charge: {totals?.ServiceChargeTotal ?? 0m:N2}";
        _grandTotalLabel.Text = $"Grand Total: {totals?.GrandTotal ?? 0m:N2}";
        _paidLabel.Text = $"Paid: {totals?.PaidTotal ?? 0m:N2}";
        _balanceLabel.Text = $"Balance: {totals?.Balance ?? 0m:N2}";
    }

    private string ResolveVariantSku(Guid variantId) => _variantsById.TryGetValue(variantId, out var v) ? v.Sku : "(unknown)";

    private string ResolveVariantName(Guid variantId) => _variantsById.TryGetValue(variantId, out var v) ? v.Name : "(unknown)";

    private string ResolveVariantDisplay(Guid variantId) => $"{ResolveVariantSku(variantId)} {ResolveVariantName(variantId)}";

    private async Task UpdatePermissionsAsync()
    {
        if (_currentSession.UserId is not { } userId)
        {
            _permissions = [];
            return;
        }

        string[] operations =
        [
            "create", "hold", "resume", "void", "cancel", "reopen", "sendtokitchen", "complete", "pay",
            "transfertable", "mergetables", "splitbill", "notes", "discount", "servicecharge", "additem", "editline",
        ];

        _permissions = new Dictionary<string, bool>();
        foreach (var operation in operations)
        {
            _permissions[operation] = await _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}");
        }
    }

    private bool Permit(string operation) => _permissions.GetValueOrDefault(operation, false);

    private void UpdateButtonStates()
    {
        var status = _currentOrder?.Status;
        var isOpen = status == "Open";
        var isHeld = status == "Held";
        var isDineIn = _currentOrder?.OrderType == "DineIn";
        var canReopen = status is "Voided" or "Cancelled";
        var hasOrder = _currentOrder is not null;

        _newDineInButton.Enabled = !hasOrder && Permit("create");
        _newTakeAwayButton.Enabled = !hasOrder && Permit("create");

        _holdButton.Enabled = isOpen && Permit("hold");
        _resumeButton.Enabled = isHeld && Permit("resume");
        _voidOrderButton.Enabled = (isOpen || isHeld) && Permit("void");
        _cancelOrderButton.Enabled = (isOpen || isHeld) && Permit("cancel");
        _reopenButton.Enabled = canReopen && Permit("reopen");
        _sendToKitchenButton.Enabled = isOpen && Permit("sendtokitchen");
        _completeButton.Enabled = isOpen && Permit("complete");
        _payButton.Enabled = isOpen && Permit("pay");
        _transferTableButton.Enabled = isOpen && isDineIn && Permit("transfertable");
        _mergeTablesButton.Enabled = isOpen && isDineIn && Permit("mergetables");
        _splitBillButton.Enabled = isOpen && isDineIn && Permit("splitbill");
        _orderNotesButton.Enabled = (isOpen || isHeld) && Permit("notes");
        _customerNotesButton.Enabled = (isOpen || isHeld) && Permit("notes");
        _addDiscountButton.Enabled = isOpen && Permit("discount");
        _removeDiscountButton.Enabled = isOpen && Permit("discount");
        _addServiceChargeButton.Enabled = isOpen && Permit("servicecharge");
        _removeServiceChargeButton.Enabled = isOpen && Permit("servicecharge");

        _addByBarcodeButton.Enabled = isOpen && Permit("additem");
        _editQuantityButton.Enabled = isOpen && Permit("editline");
        _editLineNotesButton.Enabled = isOpen && Permit("editline");
        _voidLineButton.Enabled = isOpen && Permit("editline");
        _removeLineButton.Enabled = isOpen && Permit("editline");
    }

    private sealed record OrderLineRow(
        Guid OrderLineId,
        string Sku,
        string Name,
        decimal Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        string Notes,
        bool IsVoided);
}
