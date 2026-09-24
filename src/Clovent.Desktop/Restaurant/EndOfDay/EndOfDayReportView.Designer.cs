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
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly LabelControl _titleLabel = new() { Text = "Sales Summary" };
    private readonly LabelControl _subtitleLabel = new() { Text = "Sales performance and transaction overview" };
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

    private readonly GridControl _itemsSoldGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _itemsSoldGridView = new();
    private readonly GridControl _cashSummaryGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _cashSummaryGridView = new();
    private readonly GridControl _billsGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _billsGridView = new();
    private readonly GridControl _inventoryMovementGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _inventoryMovementGridView = new();
    private readonly GridControl _stockRemainingGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _stockRemainingGridView = new();

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support. Do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "EndOfDayReportView";

        BuildGrid(_itemsSoldGrid, _itemsSoldGridView,
        [
            ("Name", "Item", 260),
            ("Quantity", "Quantity Sold", 100),
            ("Total", "Sales Amount", 120),
        ]);

        BuildGrid(_cashSummaryGrid, _cashSummaryGridView,
        [
            ("PaymentMethodName", "Payment Method", 220),
            ("Total", "Total Collected", 140),
        ]);

        BuildGrid(_billsGrid, _billsGridView,
        [
            ("OrderNumber", "Bill #", 140),
            ("CompletedAtUtc", "Completed", 170),
            ("Total", "Total", 110),
            ("PaymentMethodSummary", "Payment Method", 190),
        ]);

        BuildGrid(_inventoryMovementGrid, _inventoryMovementGridView,
        [
            ("Name", "Menu Item", 240),
            ("TransactionType", "Type", 110),
            ("Quantity", "Quantity", 100),
            ("OccurredAtUtc", "Occurred", 170),
        ]);

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

        _tabControl = new XtraTabControl { Dock = DockStyle.Fill, Padding = new Padding(12), HeaderAutoFill = DevExpress.Utils.DefaultBoolean.True };
        _tabControl.AppearancePage.Header.Font = new Font("Segoe UI", 9.5F);
        _tabControl.AppearancePage.Header.Options.UseFont = true;
        _tabControl.TabPages.Add(BuildSummaryPage());
        _tabControl.TabPages.Add(BuildGridPage("Top Selling Items", _itemsSoldGrid));
        _tabControl.TabPages.Add(BuildGridPage("Cash Summary", _cashSummaryGrid));
        _tabControl.TabPages.Add(BuildGridPage("Bills", _billsGrid));
        _tabControl.TabPages.Add(BuildGridPage("Inventory Movement", _inventoryMovementGrid));
        _tabControl.TabPages.Add(BuildGridPage("Stock Remaining", _stockRemainingGrid));
        _tabControl.SelectedPageChanged += TabControl_SelectedPageChanged;

        // ---- Header: title + muted subtitle ----
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
            Padding = new Padding(16, 14, 16, 8),
        };
        titleBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titleBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleBar.Controls.Add(_titleLabel, 0, 0);
        titleBar.Controls.Add(_subtitleLabel, 0, 1);

        // ---- Filter toolbar: quick filters + range + Generate, Print at right ----
        var periodLabel = new LabelControl { Text = "Report Period:", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        periodLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        periodLabel.Appearance.Options.UseTextOptions = true;
        periodLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        periodLabel.Appearance.ForeColor = Color.FromArgb(51, 65, 85);
        periodLabel.Appearance.Options.UseFont = true;
        periodLabel.Appearance.Options.UseForeColor = true;
        periodLabel.Padding = new Padding(0, 6, 0, 0);

        _periodCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _periodCombo.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _periodCombo.Properties.Appearance.Options.UseFont = true;
        _periodCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 9.5F);
        _periodCombo.Properties.Items.AddRange(new object[] {
            "Today",
            "Yesterday",
            "This Week",
            "Last Week",
            "This Month",
            "Last Month",
            "This Quarter",
            "Last Quarter",
            "This Year",
            "Last Year",
            "Last 7 Days",
            "Last 30 Days",
            "Custom"});
        _periodCombo.SelectedItem = "Today";

        var fromLabel = new LabelControl { Text = "From:", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        fromLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        fromLabel.Appearance.Options.UseTextOptions = true;
        fromLabel.Appearance.ForeColor = Color.Gray;
        fromLabel.Appearance.Options.UseForeColor = true;
        fromLabel.Padding = new Padding(0, 6, 0, 0);
        var toLabel = new LabelControl { Text = "To:", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        toLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        toLabel.Appearance.Options.UseTextOptions = true;
        toLabel.Appearance.ForeColor = Color.Gray;
        toLabel.Appearance.Options.UseForeColor = true;
        toLabel.Padding = new Padding(0, 6, 0, 0);

        _fromDateEdit.MinimumSize = new Size(145, 0);
        _toDateEdit.MinimumSize = new Size(145, 0);
        foreach (var button in new[] { _generateButton, _previewButton, _printButton, _exportPdfButton, _exportExcelButton, _printSummaryButton })
        {
            // AutoSize buttons grow with the DPI-scaled font, so their text
            // can never be clipped to a fixed width.
            button.AutoSize = true;
            button.MinimumSize = new Size(0, 34);
            button.Padding = new Padding(12, 4, 12, 4);
            button.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            button.Appearance.Options.UseFont = true;
            button.Cursor = Cursors.Hand;
        }
        _generateButton.MinimumSize = new Size(110, 34);
        _generateButton.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600 primary action
        _generateButton.Appearance.ForeColor = Color.White;
        _generateButton.Appearance.Options.UseBackColor = true;
        _generateButton.Appearance.Options.UseForeColor = true;

        var filterBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(16, 4, 16, 4),
        };

        void Add(Control control)
        {
            control.Margin = new Padding(4, 4, 12, 4);
            filterBar.Controls.Add(control);
        }

        Add(_warehousePicker);
        Add(periodLabel);
        Add(_periodCombo);
        Add(fromLabel);
        Add(_fromDateEdit);
        Add(toLabel);
        Add(_toDateEdit);
        Add(_generateButton);
        Add(_previewButton);
        Add(_printButton);
        Add(_exportPdfButton);
        Add(_exportExcelButton);
        Add(_printSummaryButton);

        Controls.Add(_tabControl);
        Controls.Add(filterBar);
        Controls.Add(titleBar);

        _periodCombo.SelectedIndexChanged += PeriodCombo_SelectedIndexChanged;
        _fromDateEdit.EditValueChanged += DateEdit_EditValueChanged;
        _toDateEdit.EditValueChanged += DateEdit_EditValueChanged;
        _generateButton.Click += GenerateButton_Click;
        _previewButton.Click += PreviewButton_Click;
        _printButton.Click += PrintButton_Click;
        _exportPdfButton.Click += ExportPdfButton_Click;
        _exportExcelButton.Click += ExportExcelButton_Click;
        _printSummaryButton.Click += PrintSummaryButton_Click;

        Load += EndOfDayReportView_Load;
    }

    #endregion

    private static readonly string[] MoneyFieldNames = ["Total", "Amount"];

    private static readonly string[] NumericFieldNames = ["Quantity", "QuantityOnHand", "QuantityAvailable"];

    private static void BuildGrid(GridControl grid, GridView view, (string FieldName, string Caption, int Width)[] columns)
    {
        grid.MainView = view;
        grid.ViewCollection.Add(view);
        view.OptionsBehavior.Editable = false;
        view.OptionsSelection.MultiSelect = false;
        view.OptionsView.ShowGroupPanel = false;
        view.OptionsView.EnableAppearanceEvenRow = true;
        view.OptionsView.ColumnAutoWidth = true;
        view.RowHeight = 26;

        foreach (var (fieldName, caption, width) in columns)
        {
            var column = view.Columns.AddVisible(fieldName, caption);
            column.Width = width;

            // Numeric and money columns read right-aligned, headers included,
            // so digits line up down the report like an accounting statement.
            if (MoneyFieldNames.Contains(fieldName) || NumericFieldNames.Contains(fieldName))
            {
                column.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
                column.AppearanceCell.Options.UseTextOptions = true;
                column.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
                column.AppearanceHeader.Options.UseTextOptions = true;
            }
        }

        // Every grid's "Total"/"Amount" column (Top Selling Items, Cash
        // Summary, Bills) displays as currency, and every UTC timestamp
        // column displays in the viewer's own local time (a restaurant
        // owner reading "2:45 PM" needs to not have to mentally convert
        // from UTC) - both formatting rules shared across every grid this
        // screen builds, so they're applied here once rather than repeated
        // per grid.
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
                else if (e.Value is DateTimeOffset timestamp)
                {
                    e.DisplayText = Clovent.Desktop.Forms.Base.DateTimeDisplay.Format(timestamp);
                }
            }
        };

        // Professional empty state for zero-record datasets, matching the
        // centered empty-cart convention the POS grid already established.
        view.CustomDrawEmptyForeground += (_, e) =>
        {
            e.Handled = true;
            using var font = new Font("Segoe UI", 10F);
            TextRenderer.DrawText(e.Graphics, "No data available" + Environment.NewLine + "No records were found.", font, e.Bounds, Color.Gray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
    }

    private XtraTabPage BuildSummaryPage()
    {
        var page = new XtraTabPage { Text = "Summary" };

        // Primary KPI cards: equal-width percent columns so the row adapts to
        // the screen width, and one Percent row so the cards fill the
        // container. All heights are measured from the actual (DPI-scaled)
        // fonts - this app intentionally has no AutoScaleMode, so fonts scale
        // with DPI while fixed pixel sizes do not; measured heights keep the
        // caption/value/sub-caption stack fully visible at every DPI.
        var captionFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        var valueFont = new Font("Segoe UI", 20F, FontStyle.Bold);
        var subFont = new Font("Segoe UI", 8.75F);
        var captionHeight = TextRenderer.MeasureText("Ag", captionFont).Height + 2;
        var valueHeight = TextRenderer.MeasureText("Ag", valueFont).Height + 10;
        var subHeight = TextRenderer.MeasureText("Ag", subFont).Height + 2;

        var cardsRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = captionHeight + valueHeight + subHeight + 12 + 12 + 8,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(16, 12, 16, 4),
        };
        cardsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        for (var i = 0; i < 4; i++)
        {
            cardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }
        cardsRow.Controls.Add(BuildStatCard("TOTAL BILLS", _totalBillsValueLabel, Color.FromArgb(52, 73, 94), "Transactions", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 0, 0);
        cardsRow.Controls.Add(BuildStatCard("TOTAL SALES", _totalSalesValueLabel, Color.FromArgb(41, 128, 185), "Gross Sales", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 1, 0);
        cardsRow.Controls.Add(BuildStatCard("CASH", _cashValueLabel, Color.FromArgb(39, 174, 96), "Cash Collected", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 2, 0);
        cardsRow.Controls.Add(BuildStatCard("CARD", _cardValueLabel, Color.FromArgb(142, 68, 173), "Card Collected", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 3, 0);

        // Secondary metrics: compact secondary statistics strip with clean separation
        var voidedCaption = new LabelControl { Text = "VOIDED ORDERS", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        var averageCaption = new LabelControl { Text = "AVERAGE SALE", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        foreach (var caption in new[] { voidedCaption, averageCaption })
        {
            caption.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            caption.Appearance.ForeColor = Color.FromArgb(100, 116, 139);
            caption.Appearance.Options.UseFont = true;
            caption.Appearance.Options.UseForeColor = true;
            caption.Margin = new Padding(16, 8, 8, 8);
        }

        foreach (var label in new[] { _voidedCountLabel, _averageSaleLabel })
        {
            label.Text = "0";
            label.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(30, 41, 59);
            label.AutoSize = true;
            label.Margin = new Padding(0, 7, 32, 8);
        }

        var secondary = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(16, 4, 16, 8),
            Margin = new Padding(0)
        };
        secondary.Controls.Add(voidedCaption);
        secondary.Controls.Add(_voidedCountLabel);
        secondary.Controls.Add(averageCaption);
        secondary.Controls.Add(_averageSaleLabel);

        // Professional empty state for the report body instead of a blank
        // white area; toggled by GenerateCoreAsync from the real data.
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

    /// <summary>One dashboard KPI card - caption on top, large accent-colored value, muted sub-caption underneath; equal 25% width so the row never clips.</summary>
    private static PanelControl BuildStatCard(string caption, LabelControl valueLabel, Color accentColor, string subCaption, Font captionFont, Font valueFont, Font subFont, int captionHeight, int valueHeight, int subHeight)
    {
        var card = new PanelControl
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 12, 14, 12),
            Margin = new Padding(4, 4, 4, 4),
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
}
