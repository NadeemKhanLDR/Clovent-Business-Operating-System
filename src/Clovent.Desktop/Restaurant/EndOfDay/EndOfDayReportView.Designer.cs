using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.MasterData;
using Clovent.Restaurant.Application.EndOfDay.Dtos;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;

namespace Clovent.Desktop.Restaurant.EndOfDay;

partial class EndOfDayReportView
{
    private System.ComponentModel.IContainer components = null;

    private readonly LabelControl _titleLabel = new() { Text = "Sales Summary" };
    private readonly LabelControl _subtitleLabel = new() { Text = "Sales performance and comprehensive transaction overview" };
    private readonly EntityPicker _warehousePicker = new("Location:");
    private readonly ComboBoxEdit _periodCombo = new();
    private readonly DateEdit _fromDateEdit = new() { EditValue = DateTime.Today };
    private readonly DateEdit _toDateEdit = new() { EditValue = DateTime.Today };
    private readonly SimpleButton _generateButton = new() { Text = "Generate" };
    private readonly SimpleButton _previewButton = new() { Text = "Preview" };
    private readonly SimpleButton _printButton = new() { Text = "Print" };
    private readonly SimpleButton _exportPdfButton = new() { Text = "Export PDF" };
    private readonly SimpleButton _exportExcelButton = new() { Text = "Export Excel" };

    private readonly LabelControl _totalBillsValueLabel = new();
    private readonly LabelControl _totalSalesValueLabel = new();
    private readonly LabelControl _cashValueLabel = new();
    private readonly LabelControl _cardValueLabel = new();
    private readonly LabelControl _voidedCountLabel = new();
    private readonly LabelControl _averageSaleLabel = new();
    private readonly SimpleButton _printSummaryButton = new() { Text = "Print Summary" };
    private readonly LabelControl _summaryEmptyStateLabel = new();

    private XtraTabControl _tabControl = null!;

    // 10 grids for the 10 data tabs (Tab 0 is the Summary KPI dashboard)
    private readonly GridControl _ordersGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _ordersGridView = new();
    private readonly GridView _ordersDetailView = new();

    private readonly GridControl _itemsGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _itemsGridView = new();

    private readonly GridControl _customersGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _customersGridView = new();

    private readonly GridControl _paymentsGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _paymentsGridView = new();

    private readonly GridControl _receivablesGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _receivablesGridView = new();

    private readonly GridControl _orderTypesGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _orderTypesGridView = new();

    private readonly GridControl _itemTypesGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _itemTypesGridView = new();

    private readonly GridControl _cashSummaryGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _cashSummaryGridView = new();

    private readonly GridControl _inventoryMovementGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _inventoryMovementGridView = new();

    private readonly GridControl _stockRemainingGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _stockRemainingGridView = new();

    #region Component Designer generated code

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "EndOfDayReportView";

        // Tab 1: Orders / Bills (with master-detail lines)
        BuildGrid(_ordersGrid, _ordersGridView,
        [
            ("OrderNumber", "Order #", 120),
            ("OrderType", "Type", 75),
            ("OrderSource", "Source", 75),
            ("CustomerName", "Customer", 130),
            ("TableOrRider", "Table / Rider", 100),
            ("ItemsCount", "Items", 55),
            ("Subtotal", "Subtotal", 90),
            ("Discount", "Discount", 75),
            ("ServiceAndDeliveryFee", "Fee/Service", 85),
            ("Tax", "Tax", 75),
            ("Total", "Total", 95),
            ("PaidAmount", "Paid", 85),
            ("OnAccountAmount", "On Account", 85),
            ("PaymentSummary", "Payment Method", 130),
            ("Status", "Status", 75),
            ("CreatedAtUtc", "Time", 120)
        ]);
        BuildDetailGrid(_ordersGrid, _ordersDetailView, "Lines",
        [
            ("ItemName", "Item", 180),
            ("VariantName", "Variant", 110),
            ("ItemType", "Type", 90),
            ("Quantity", "Qty", 65),
            ("UnitPrice", "Unit Price", 85),
            ("Discount", "Discount", 75),
            ("Tax", "Tax", 75),
            ("LineTotal", "Line Total", 95)
        ]);
        _ordersGridView.OptionsDetail.EnableMasterViewMode = true;
        _ordersGridView.OptionsDetail.ShowDetailTabs = false;
        _ordersGridView.DoubleClick += OrdersGridView_DoubleClick;

        // Tab 2: Items
        BuildGrid(_itemsGrid, _itemsGridView,
        [
            ("CategoryName", "Category", 110),
            ("ItemName", "Item", 170),
            ("ItemType", "Classification", 90),
            ("QuantitySold", "Qty Sold", 75),
            ("UnitPrice", "Avg Price", 85),
            ("CostPrice", "Unit Cost", 85),
            ("TotalSales", "Sales Amount", 100),
            ("EstimatedCost", "Cost Amount", 95),
            ("GrossProfit", "Gross Profit", 95),
            ("MarginPercent", "Margin %", 75),
            ("PercentOfTotalSales", "Share %", 70)
        ]);

        // Tab 3: Customers
        BuildGrid(_customersGrid, _customersGridView,
        [
            ("CustomerCode", "Code", 75),
            ("CustomerName", "Customer Name", 160),
            ("MobileNumber", "Mobile", 95),
            ("OrdersCount", "Orders", 65),
            ("NetSales", "Sales", 95),
            ("TotalPaid", "Paid", 95),
            ("OnAccountIncurred", "On Account", 95),
            ("AccountPaymentsCollected", "Payments Recv", 105),
            ("EndingReceivable", "A/R Balance", 100),
            ("AdvanceBalance", "Advance", 95)
        ]);

        // Tab 4: Payments
        BuildGrid(_paymentsGrid, _paymentsGridView,
        [
            ("Category", "Category", 120),
            ("PaymentMethodName", "Payment Method", 160),
            ("TransactionsCount", "Transactions", 95),
            ("TotalCollected", "Amount Collected", 130),
            ("PercentOfTotal", "Share %", 80)
        ]);

        // Tab 5: Receivables Movement
        BuildGrid(_receivablesGrid, _receivablesGridView,
        [
            ("CustomerCode", "Code", 75),
            ("CustomerName", "Customer Name", 160),
            ("OpeningReceivable", "Opening A/R", 95),
            ("NewOnAccountSales", "+ On Account", 95),
            ("CustomerPayments", "- Collections", 95),
            ("AdvanceApplied", "- Adv Applied", 95),
            ("ClosingReceivable", "= Closing A/R", 100),
            ("OpeningAdvance", "Open Adv", 85),
            ("AdvanceReceived", "+ Adv Recv", 85),
            ("AdvanceUsed", "- Adv Used", 85),
            ("ClosingAdvance", "= Close Adv", 90)
        ]);

        // Tab 6: Order Types
        BuildGrid(_orderTypesGrid, _orderTypesGridView,
        [
            ("OrderType", "Order Type", 130),
            ("OrdersCount", "Orders", 80),
            ("QuantitySold", "Items Sold", 90),
            ("DeliveryFees", "Delivery Fees", 100),
            ("TotalSales", "Total Sales", 120),
            ("AverageOrderValue", "Avg Order Value", 110),
            ("PercentOfTotal", "Share %", 85)
        ]);

        // Tab 7: Item Types / Profitability
        BuildGrid(_itemTypesGrid, _itemTypesGridView,
        [
            ("ItemType", "Classification", 140),
            ("QuantitySold", "Quantity Sold", 100),
            ("TotalSales", "Total Sales", 120),
            ("CostDisplay", "Cost Description", 160),
            ("TotalCost", "Total Cost", 100),
            ("GrossProfit", "Gross Profit", 110),
            ("MarginPercent", "Margin %", 85)
        ]);

        // Tab 8: Cash Summary
        BuildGrid(_cashSummaryGrid, _cashSummaryGridView,
        [
            ("PaymentMethodName", "Payment Method", 200),
            ("Total", "Total Collected", 130),
        ]);

        // Tab 9: Inventory Movement
        BuildGrid(_inventoryMovementGrid, _inventoryMovementGridView,
        [
            ("Name", "Menu Item", 240),
            ("TransactionType", "Type", 110),
            ("Quantity", "Quantity", 100),
            ("OccurredAtUtc", "Occurred", 170),
        ]);

        // Tab 10: Stock Remaining
        BuildGrid(_stockRemainingGrid, _stockRemainingGridView,
        [
            ("Name", "Menu Item", 240),
            ("QuantityOnHand", "On Hand", 100),
            ("QuantityAvailable", "Available", 100),
        ]);
        _stockRemainingGridView.RowStyle += (_, e) =>
        {
            if (_stockRemainingGridView.GetRow(e.RowHandle) is StockRow row && row.QuantityAvailable <= 0)
            {
                e.Appearance.ForeColor = Color.FromArgb(192, 57, 43);
                e.Appearance.Options.UseForeColor = true;
            }
        };

        _tabControl = new XtraTabControl { Dock = DockStyle.Fill, Padding = new Padding(8), HeaderAutoFill = DevExpress.Utils.DefaultBoolean.False };
        _tabControl.AppearancePage.Header.Font = new Font("Segoe UI", 9F);
        _tabControl.AppearancePage.Header.Options.UseFont = true;

        // Add all 11 tabs
        _tabControl.TabPages.Add(BuildSummaryPage());
        _tabControl.TabPages.Add(BuildGridPage("Orders / Bills", _ordersGrid));
        _tabControl.TabPages.Add(BuildGridPage("Items", _itemsGrid));
        _tabControl.TabPages.Add(BuildGridPage("Customers", _customersGrid));
        _tabControl.TabPages.Add(BuildGridPage("Payments", _paymentsGrid));
        _tabControl.TabPages.Add(BuildGridPage("Receivables Movement", _receivablesGrid));
        _tabControl.TabPages.Add(BuildGridPage("Order Types", _orderTypesGrid));
        _tabControl.TabPages.Add(BuildGridPage("Item Types / Profitability", _itemTypesGrid));
        _tabControl.TabPages.Add(BuildGridPage("Cash Summary", _cashSummaryGrid));
        _tabControl.TabPages.Add(BuildGridPage("Inventory Movement", _inventoryMovementGrid));
        _tabControl.TabPages.Add(BuildGridPage("Stock Remaining", _stockRemainingGrid));
        _tabControl.SelectedPageChanged += TabControl_SelectedPageChanged;

        // Header: title + muted subtitle
        _titleLabel.Appearance.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        _titleLabel.Appearance.Options.UseFont = true;
        _subtitleLabel.Appearance.Font = new Font("Segoe UI", 9.5F);
        _subtitleLabel.Appearance.ForeColor = Color.Gray;
        _subtitleLabel.Appearance.Options.UseFont = true;
        _subtitleLabel.Appearance.Options.UseForeColor = true;

        var titleBar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16, 12, 16, 6),
        };
        titleBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titleBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleBar.Controls.Add(_titleLabel, 0, 0);
        titleBar.Controls.Add(_subtitleLabel, 0, 1);

        // Filter toolbar
        var periodLabel = new LabelControl { Text = "Period:", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        periodLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        periodLabel.Appearance.ForeColor = Color.FromArgb(51, 65, 85);
        periodLabel.Appearance.Options.UseFont = true;
        periodLabel.Appearance.Options.UseForeColor = true;
        periodLabel.Padding = new Padding(0, 6, 0, 0);

        var fromLabel = new LabelControl { Text = "From:", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        fromLabel.Appearance.ForeColor = Color.Gray;
        fromLabel.Appearance.Options.UseForeColor = true;
        fromLabel.Padding = new Padding(0, 6, 0, 0);

        var toLabel = new LabelControl { Text = "To:", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        toLabel.Appearance.ForeColor = Color.Gray;
        toLabel.Appearance.Options.UseForeColor = true;
        toLabel.Padding = new Padding(0, 6, 0, 0);

        _periodCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _periodCombo.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _periodCombo.Properties.Appearance.Options.UseFont = true;
        _periodCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 9.5F);
        _periodCombo.Properties.AppearanceDropDown.Options.UseFont = true;
        _periodCombo.Size = new Size(180, 32);

        _fromDateEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _fromDateEdit.Properties.Appearance.Options.UseFont = true;
        _fromDateEdit.Size = new Size(160, 32);
        _fromDateEdit.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        _fromDateEdit.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
        _fromDateEdit.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        _fromDateEdit.Properties.EditFormat.FormatString = "dd-MMM-yyyy";
        _fromDateEdit.Properties.Mask.EditMask = "dd-MMM-yyyy";

        _toDateEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _toDateEdit.Properties.Appearance.Options.UseFont = true;
        _toDateEdit.Size = new Size(160, 32);
        _toDateEdit.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        _toDateEdit.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
        _toDateEdit.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        _toDateEdit.Properties.EditFormat.FormatString = "dd-MMM-yyyy";
        _toDateEdit.Properties.Mask.EditMask = "dd-MMM-yyyy";

        foreach (var button in new[] { _generateButton, _previewButton, _printButton, _exportPdfButton, _exportExcelButton, _printSummaryButton })
        {
            button.AutoSize = true;
            button.MinimumSize = new Size(0, 32);
            button.Padding = new Padding(10, 4, 10, 4);
            button.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Appearance.Options.UseFont = true;
            button.Cursor = Cursors.Hand;
        }

        _generateButton.MinimumSize = new Size(100, 32);
        _generateButton.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _generateButton.Appearance.ForeColor = Color.White;
        _generateButton.Appearance.Options.UseBackColor = true;
        _generateButton.Appearance.Options.UseForeColor = true;

        _printSummaryButton.MinimumSize = new Size(120, 32);
        _printSummaryButton.Appearance.BackColor = Color.FromArgb(71, 85, 105);
        _printSummaryButton.Appearance.ForeColor = Color.White;
        _printSummaryButton.Appearance.Options.UseBackColor = true;
        _printSummaryButton.Appearance.Options.UseForeColor = true;

        var filterBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(16, 2, 16, 4),
        };

        void AddControl(Control c)
        {
            c.Margin = new Padding(4, 3, 8, 3);
            filterBar.Controls.Add(c);
        }

        AddControl(_warehousePicker);
        AddControl(periodLabel);
        AddControl(_periodCombo);
        AddControl(fromLabel);
        AddControl(_fromDateEdit);
        AddControl(toLabel);
        AddControl(_toDateEdit);
        AddControl(_generateButton);
        AddControl(_previewButton);
        AddControl(_printButton);
        AddControl(_exportPdfButton);
        AddControl(_exportExcelButton);
        AddControl(_printSummaryButton);

        Controls.Add(_tabControl);
        Controls.Add(filterBar);
        Controls.Add(titleBar);

        _periodCombo.SelectedIndexChanged += PeriodCombo_SelectedIndexChanged;
        _fromDateEdit.EditValueChanged += DateEdit_EditValueChanged;
        _toDateEdit.EditValueChanged += DateEdit_EditValueChanged;
        _generateButton.Click += GenerateButton_Click;
        _printSummaryButton.Click += PrintSummaryButton_Click;
        _previewButton.Click += PreviewButton_Click;
        _printButton.Click += PrintButton_Click;
        _exportPdfButton.Click += ExportPdfButton_Click;
        _exportExcelButton.Click += ExportExcelButton_Click;

        Load += EndOfDayReportView_Load;
    }

    #endregion

    private static readonly string[] MoneyFieldNames =
    [
        "Total", "Amount", "TotalSales", "TotalCost", "GrossProfit", "UnitPrice", "CostPrice",
        "EstimatedCost", "Subtotal", "Discount", "ServiceAndDeliveryFee", "Tax", "PaidAmount",
        "OnAccountAmount", "OutstandingAmount", "NetSales", "TotalPaid", "OnAccountIncurred",
        "AccountPaymentsCollected", "EndingReceivable", "AdvanceBalance", "TotalCollected",
        "OpeningReceivable", "NewOnAccountSales", "CustomerPayments", "AdvanceApplied",
        "ClosingReceivable", "OpeningAdvance", "AdvanceReceived", "AdvanceUsed", "ClosingAdvance",
        "DeliveryFees", "AverageOrderValue", "LineTotal", "ServiceCharge"
    ];

    private static readonly string[] NumericFieldNames =
    [
        "Quantity", "QuantityOnHand", "QuantityAvailable", "QuantitySold", "OrdersCount",
        "ItemsCount", "TransactionsCount"
    ];

    private static void BuildGrid(GridControl grid, GridView view, (string FieldName, string Caption, int Width)[] columns)
    {
        grid.MainView = view;
        grid.ViewCollection.Add(view);
        view.OptionsBehavior.Editable = false;
        view.OptionsSelection.MultiSelect = false;
        view.OptionsView.ShowGroupPanel = false;
        view.OptionsView.EnableAppearanceEvenRow = true;
        view.OptionsView.ColumnAutoWidth = true;
        view.OptionsView.ShowFooter = true;
        view.RowHeight = 26;

        bool first = true;
        foreach (var (fieldName, caption, width) in columns)
        {
            var column = view.Columns.AddVisible(fieldName, caption);
            column.Width = width;

            if (first)
            {
                column.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Custom;
                column.SummaryItem.DisplayFormat = "Total";
                first = false;
            }

            if (MoneyFieldNames.Contains(fieldName) || NumericFieldNames.Contains(fieldName) || fieldName.EndsWith("Percent"))
            {
                column.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
                column.AppearanceCell.Options.UseTextOptions = true;
                column.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
                column.AppearanceHeader.Options.UseTextOptions = true;

                if (MoneyFieldNames.Contains(fieldName))
                {
                    column.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                    column.SummaryItem.DisplayFormat = "{0:n2}";
                }
                else if (NumericFieldNames.Contains(fieldName))
                {
                    column.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                    column.SummaryItem.DisplayFormat = "{0:n0}";
                }
            }
        }

        view.CustomColumnDisplayText += (_, e) =>
        {
            if (e.Value != null && e.Value != DBNull.Value)
            {
                if (MoneyFieldNames.Contains(e.Column.FieldName))
                {
                    try
                    {
                        var amount = Convert.ToDecimal(e.Value);
                        e.DisplayText = CurrencyDisplay.FormatPlain(amount);
                    }
                    catch { }
                }
                else if (e.Column.FieldName.EndsWith("Percent"))
                {
                    try
                    {
                        var pct = Convert.ToDecimal(e.Value);
                        e.DisplayText = $"{pct:F1}%";
                    }
                    catch { }
                }
                else if (NumericFieldNames.Contains(e.Column.FieldName))
                {
                    try
                    {
                        var quantity = Convert.ToDecimal(e.Value);
                        e.DisplayText = quantity.ToString("0.##");
                    }
                    catch { }
                }
                else if (e.Value is DateTimeOffset timestamp)
                {
                    e.DisplayText = DateTimeDisplay.Format(timestamp);
                }
            }
        };

        view.CustomDrawEmptyForeground += (_, e) =>
        {
            if (view.RowCount > 0) return;
            e.Handled = true;
            using var font = new Font("Segoe UI", 10F);
            TextRenderer.DrawText(e.Graphics, "No records found for the selected period.", font, e.Bounds, Color.Gray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
    }

    private static void BuildDetailGrid(GridControl grid, GridView view, string relationName, (string FieldName, string Caption, int Width)[] columns)
    {
        grid.LevelTree.Nodes.Add(relationName, view);
        grid.ViewCollection.Add(view);
        view.GridControl = grid;
        view.OptionsBehavior.Editable = false;
        view.OptionsSelection.MultiSelect = false;
        view.OptionsView.ShowGroupPanel = false;
        view.OptionsView.EnableAppearanceEvenRow = true;
        view.OptionsView.ColumnAutoWidth = true;
        view.OptionsView.ShowFooter = true;
        view.RowHeight = 24;

        bool first = true;
        foreach (var (fieldName, caption, width) in columns)
        {
            var column = view.Columns.AddVisible(fieldName, caption);
            column.Width = width;

            if (first)
            {
                column.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Custom;
                column.SummaryItem.DisplayFormat = "Total";
                first = false;
            }

            if (MoneyFieldNames.Contains(fieldName) || NumericFieldNames.Contains(fieldName))
            {
                column.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
                column.AppearanceCell.Options.UseTextOptions = true;
                column.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
                column.AppearanceHeader.Options.UseTextOptions = true;

                if (MoneyFieldNames.Contains(fieldName))
                {
                    column.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                    column.SummaryItem.DisplayFormat = "{0:n2}";
                }
                else if (NumericFieldNames.Contains(fieldName))
                {
                    column.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                    column.SummaryItem.DisplayFormat = "{0:n0}";
                }
            }
        }

        view.CustomColumnDisplayText += (_, e) =>
        {
            if (e.Value != null && e.Value != DBNull.Value)
            {
                if (MoneyFieldNames.Contains(e.Column.FieldName))
                {
                    try
                    {
                        var amount = Convert.ToDecimal(e.Value);
                        e.DisplayText = CurrencyDisplay.FormatPlain(amount);
                    }
                    catch { }
                }
                else if (NumericFieldNames.Contains(e.Column.FieldName))
                {
                    try
                    {
                        var quantity = Convert.ToDecimal(e.Value);
                        e.DisplayText = quantity.ToString("0.##");
                    }
                    catch { }
                }
            }
        };
    }

    private XtraTabPage BuildSummaryPage()
    {
        var page = new XtraTabPage { Text = "Summary", Padding = new Padding(12) };

        var captionFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        var valueFont = new Font("Segoe UI", 18F, FontStyle.Bold);
        var subFont = new Font("Segoe UI", 8.25F);
        var captionHeight = TextRenderer.MeasureText("Ag", captionFont).Height + 2;
        var valueHeight = TextRenderer.MeasureText("Ag", valueFont).Height + 8;
        var subHeight = TextRenderer.MeasureText("Ag", subFont).Height + 2;

        var cardsRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = captionHeight + valueHeight + subHeight + 30,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(12, 6, 12, 6),
        };
        cardsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        for (var i = 0; i < 4; i++)
        {
            cardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        cardsRow.Controls.Add(BuildStatCard("TOTAL BILLS", _totalBillsValueLabel, Color.FromArgb(41, 128, 185), "Bills generated this period", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 0, 0);
        cardsRow.Controls.Add(BuildStatCard("TOTAL SALES", _totalSalesValueLabel, Color.FromArgb(39, 174, 96), "Gross sales across all tenders", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 1, 0);
        cardsRow.Controls.Add(BuildStatCard("CASH COLLECTED", _cashValueLabel, Color.FromArgb(230, 126, 34), "Cash payments received", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 2, 0);
        cardsRow.Controls.Add(BuildStatCard("CARD COLLECTED", _cardValueLabel, Color.FromArgb(142, 68, 173), "Electronic / card payments received", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 3, 0);

        var voidedCaption = new LabelControl { Text = "Voided Orders:" };
        voidedCaption.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        voidedCaption.Appearance.ForeColor = Color.FromArgb(51, 65, 85);
        voidedCaption.Appearance.Options.UseFont = true;
        voidedCaption.Appearance.Options.UseForeColor = true;

        var averageCaption = new LabelControl { Text = "Average Sale / Bill:" };
        averageCaption.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        averageCaption.Appearance.ForeColor = Color.FromArgb(51, 65, 85);
        averageCaption.Appearance.Options.UseFont = true;
        averageCaption.Appearance.Options.UseForeColor = true;

        foreach (var label in new[] { voidedCaption, _voidedCountLabel, averageCaption, _averageSaleLabel })
        {
            label.Appearance.Font = new Font("Segoe UI", 9.5F);
            label.Appearance.Options.UseFont = true;
            label.AutoSize = true;
            label.Margin = new Padding(0, 6, 24, 6);
        }

        var secondary = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(16, 4, 16, 6),
            Margin = new Padding(0)
        };
        secondary.Controls.Add(voidedCaption);
        secondary.Controls.Add(_voidedCountLabel);
        secondary.Controls.Add(averageCaption);
        secondary.Controls.Add(_averageSaleLabel);

        _summaryEmptyStateLabel.Text = "No summary data available\n\nNo sales transactions were found for the selected date range.";
        _summaryEmptyStateLabel.Dock = DockStyle.Fill;
        _summaryEmptyStateLabel.AutoSizeMode = LabelAutoSizeMode.None;
        _summaryEmptyStateLabel.Appearance.Font = new Font("Segoe UI", 10F);
        _summaryEmptyStateLabel.Appearance.ForeColor = Color.Gray;
        _summaryEmptyStateLabel.Appearance.Options.UseFont = true;
        _summaryEmptyStateLabel.Appearance.Options.UseForeColor = true;
        _summaryEmptyStateLabel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
        _summaryEmptyStateLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _summaryEmptyStateLabel.Appearance.Options.UseTextOptions = true;
        _summaryEmptyStateLabel.Visible = false;

        page.Controls.Add(_summaryEmptyStateLabel);
        page.Controls.Add(secondary);
        page.Controls.Add(cardsRow);
        return page;
    }

    private static PanelControl BuildStatCard(string caption, LabelControl valueLabel, Color accentColor, string subCaption, Font captionFont, Font valueFont, Font subFont, int captionHeight, int valueHeight, int subHeight)
    {
        var card = new PanelControl
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(4, 2, 4, 2),
        };
        card.Appearance.BorderColor = Color.Gainsboro;
        card.Appearance.Options.UseBorderColor = true;

        var captionLabel = new LabelControl { Text = caption, Dock = DockStyle.Top, Height = captionHeight, AutoSizeMode = LabelAutoSizeMode.None };
        captionLabel.Appearance.Font = captionFont;
        captionLabel.Appearance.ForeColor = Color.Gray;
        captionLabel.Appearance.Options.UseFont = true;
        captionLabel.Appearance.Options.UseForeColor = true;

        valueLabel.Text = "0.00";
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Height = valueHeight;
        valueLabel.AutoSizeMode = LabelAutoSizeMode.None;
        valueLabel.Appearance.Font = valueFont;
        valueLabel.Appearance.ForeColor = accentColor;
        valueLabel.Appearance.Options.UseFont = true;
        valueLabel.Appearance.Options.UseForeColor = true;
        valueLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        valueLabel.Appearance.Options.UseTextOptions = true;

        var subLabel = new LabelControl { Text = subCaption, Dock = DockStyle.Bottom, Height = subHeight, AutoSizeMode = LabelAutoSizeMode.None };
        subLabel.Appearance.Font = subFont;
        subLabel.Appearance.ForeColor = Color.Silver;
        subLabel.Appearance.Options.UseFont = true;
        subLabel.Appearance.Options.UseForeColor = true;

        card.Controls.Add(valueLabel);
        card.Controls.Add(captionLabel);
        card.Controls.Add(subLabel);
        return card;
    }

    private static XtraTabPage BuildGridPage(string title, GridControl grid)
    {
        var page = new XtraTabPage { Text = title };
        grid.Dock = DockStyle.Fill;
        page.Controls.Add(grid);
        return page;
    }

    private void ExportGrid(GridControl grid, string filter, string fileName, Action<GridControl, string> export)
    {
        using var dialog = new SaveFileDialog { Filter = filter, FileName = fileName };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            export(grid, dialog.FileName);
        }
    }

    public void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        int editorH = DesktopDpi.Scale(32, this);
        _periodCombo.MinimumSize = new Size(DesktopDpi.Scale(180, this), editorH);
        _periodCombo.Size = _periodCombo.MinimumSize;

        _fromDateEdit.MinimumSize = new Size(DesktopDpi.Scale(160, this), editorH);
        _fromDateEdit.Size = _fromDateEdit.MinimumSize;

        _toDateEdit.MinimumSize = new Size(DesktopDpi.Scale(160, this), editorH);
        _toDateEdit.Size = _toDateEdit.MinimumSize;

        _generateButton.MinimumSize = new Size(DesktopDpi.Scale(100, this), editorH);
        _previewButton.MinimumSize = new Size(DesktopDpi.Scale(90, this), editorH);
        _printButton.MinimumSize = new Size(DesktopDpi.Scale(90, this), editorH);
        _exportPdfButton.MinimumSize = new Size(DesktopDpi.Scale(100, this), editorH);
        _exportExcelButton.MinimumSize = new Size(DesktopDpi.Scale(100, this), editorH);
        _printSummaryButton.MinimumSize = new Size(DesktopDpi.Scale(120, this), editorH);

        var allViews = new[]
        {
            _ordersGridView, _ordersDetailView, _itemsGridView, _customersGridView,
            _paymentsGridView, _receivablesGridView, _orderTypesGridView, _itemTypesGridView,
            _cashSummaryGridView, _inventoryMovementGridView, _stockRemainingGridView
        };

        int rowH = DesktopDpi.Scale(28, this);
        int headerH = DesktopDpi.Scale(32, this);
        foreach (var view in allViews)
        {
            view.RowHeight = rowH;
            view.ColumnPanelRowHeight = headerH;
        }
    }
}
