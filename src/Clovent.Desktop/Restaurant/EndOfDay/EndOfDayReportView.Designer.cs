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
    // Captioned "Location", not "Warehouse" - see RestaurantPosView's own
    // _warehousePicker field comment/RestaurantPOSArchitecture.md Section 15.
    private readonly EntityPicker _warehousePicker = new("Location:");
    private readonly SimpleButton _todayButton = new() { Text = "Today" };
    private readonly SimpleButton _yesterdayButton = new() { Text = "Yesterday" };
    private readonly DateEdit _fromDateEdit = new() { EditValue = DateTime.UtcNow.Date };
    private readonly DateEdit _toDateEdit = new() { EditValue = DateTime.UtcNow.Date };
    private readonly SimpleButton _generateButton = new() { Text = "Generate" };

    private readonly LabelControl _totalBillsValueLabel = new();
    private readonly LabelControl _totalSalesValueLabel = new();
    private readonly LabelControl _cashValueLabel = new();
    private readonly LabelControl _cardValueLabel = new();
    private readonly LabelControl _voidedCountLabel = new();
    private readonly LabelControl _averageSaleLabel = new();
    private readonly SimpleButton _printSummaryButton = new() { Text = "Print Summary" };

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
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "EndOfDayReportView";

        BuildGrid(_itemsSoldGrid, _itemsSoldGridView,
        [
            ("Name", "Menu Item", 260),
            ("Quantity", "Quantity", 90),
            ("Total", "Total", 100),
        ]);

        BuildGrid(_cashSummaryGrid, _cashSummaryGridView,
        [
            ("PaymentMethodName", "Payment Method", 180),
            ("Total", "Total", 120),
        ]);

        BuildGrid(_billsGrid, _billsGridView,
        [
            ("OrderNumber", "Bill #", 140),
            ("CompletedAtUtc", "Completed", 160),
            ("Total", "Total", 100),
            ("PaymentMethodSummary", "Payment Method", 160),
        ]);

        BuildGrid(_inventoryMovementGrid, _inventoryMovementGridView,
        [
            ("Name", "Menu Item", 220),
            ("TransactionType", "Type", 100),
            ("Quantity", "Quantity", 90),
            ("OccurredAtUtc", "Occurred", 160),
        ]);

        BuildGrid(_stockRemainingGrid, _stockRemainingGridView,
        [
            ("Name", "Menu Item", 220),
            ("QuantityOnHand", "On Hand", 90),
            ("QuantityAvailable", "Available", 90),
        ]);

        var tabControl = new XtraTabControl { Dock = DockStyle.Fill };
        tabControl.TabPages.Add(BuildSummaryPage());
        tabControl.TabPages.Add(BuildGridPage("Top Selling Items", _itemsSoldGrid, "itemssold"));
        tabControl.TabPages.Add(BuildGridPage("Cash Summary", _cashSummaryGrid, "cashsummary"));
        tabControl.TabPages.Add(BuildGridPage("Bills", _billsGrid, "bills"));
        tabControl.TabPages.Add(BuildGridPage("Inventory Movement", _inventoryMovementGrid, "inventorymovement"));
        tabControl.TabPages.Add(BuildGridPage("Stock Remaining", _stockRemainingGrid, "stockremaining"));

        _titleLabel.Appearance.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        _titleLabel.Appearance.Options.UseFont = true;

        var titleBar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(12, 10, 12, 0) };
        titleBar.Controls.Add(_titleLabel);

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(4) };
        _fromDateEdit.Width = 110;
        _toDateEdit.Width = 110;
        topPanel.Controls.Add(_warehousePicker);
        topPanel.Controls.Add(_todayButton);
        topPanel.Controls.Add(_yesterdayButton);
        topPanel.Controls.Add(new LabelControl { Text = "From:", Padding = new Padding(8, 6, 4, 0) });
        topPanel.Controls.Add(_fromDateEdit);
        topPanel.Controls.Add(new LabelControl { Text = "To:", Padding = new Padding(8, 6, 4, 0) });
        topPanel.Controls.Add(_toDateEdit);
        topPanel.Controls.Add(_generateButton);

        Controls.Add(tabControl);
        Controls.Add(topPanel);
        Controls.Add(titleBar);

        _todayButton.Click += TodayButton_Click;
        _yesterdayButton.Click += YesterdayButton_Click;
        _generateButton.Click += GenerateButton_Click;
        _printSummaryButton.Click += PrintSummaryButton_Click;

        Load += EndOfDayReportView_Load;
    }

    #endregion

    private static readonly string[] MoneyFieldNames = ["Total", "Amount"];

    private static void BuildGrid(GridControl grid, GridView view, (string FieldName, string Caption, int Width)[] columns)
    {
        grid.MainView = view;
        grid.ViewCollection.Add(view);
        view.OptionsBehavior.Editable = false;
        view.OptionsSelection.MultiSelect = false;
        view.OptionsView.ShowGroupPanel = false;

        foreach (var (fieldName, caption, width) in columns)
        {
            view.Columns.AddVisible(fieldName, caption).Width = width;
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
            if (MoneyFieldNames.Contains(e.Column.FieldName) && e.Value is decimal amount)
            {
                e.DisplayText = CurrencyDisplay.Format(amount);
            }
            else if (e.Value is DateTimeOffset timestamp)
            {
                e.DisplayText = timestamp.ToLocalTime().ToString("MMM d, yyyy h:mm tt");
            }
        };
    }

    private XtraTabPage BuildSummaryPage()
    {
        var page = new XtraTabPage { Text = "Summary" };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12) };

        var cardsRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Margin = new Padding(0, 0, 0, 12) };
        cardsRow.Controls.Add(BuildStatCard("Total Bills", _totalBillsValueLabel, Color.FromArgb(52, 73, 94)));
        cardsRow.Controls.Add(BuildStatCard("Total Sales", _totalSalesValueLabel, Color.FromArgb(41, 128, 185)));
        cardsRow.Controls.Add(BuildStatCard("Cash", _cashValueLabel, Color.FromArgb(39, 174, 96)));
        cardsRow.Controls.Add(BuildStatCard("Card", _cardValueLabel, Color.FromArgb(142, 68, 173)));
        panel.Controls.Add(cardsRow);

        foreach (var label in new[] { _voidedCountLabel, _averageSaleLabel })
        {
            label.Font = new Font(Font.FontFamily, 10f);
            label.ForeColor = Color.Gray;
            panel.Controls.Add(label);
        }

        panel.Controls.Add(new SeparatorControl { Width = 260, Margin = new Padding(0, 8, 0, 8) });
        panel.Controls.Add(_printSummaryButton);

        page.Controls.Add(panel);
        return page;
    }

    /// <summary>One "Total Bills"/"Total Sales"/"Cash"/"Card" stat tile - a bordered panel with a large accent-colored value and a caption underneath, so an owner reads the day's headline numbers at a glance instead of a stacked list of labels.</summary>
    private static PanelControl BuildStatCard(string caption, LabelControl valueLabel, Color accentColor)
    {
        var card = new PanelControl
        {
            Width = 160,
            Height = 84,
            Padding = new Padding(12, 10, 12, 10),
            Margin = new Padding(0, 0, 12, 12),
        };
        card.Appearance.BorderColor = Color.Gainsboro;
        card.Appearance.Options.UseBorderColor = true;

        valueLabel.Text = "0.00";
        valueLabel.Dock = DockStyle.Top;
        valueLabel.Height = 36;
        valueLabel.Appearance.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
        valueLabel.Appearance.ForeColor = accentColor;
        valueLabel.Appearance.Options.UseFont = true;
        valueLabel.Appearance.Options.UseForeColor = true;

        var captionLabel = new LabelControl { Text = caption, Dock = DockStyle.Bottom, Height = 22 };
        captionLabel.Appearance.Font = new Font("Segoe UI", 9.5F);
        captionLabel.Appearance.ForeColor = Color.Gray;
        captionLabel.Appearance.Options.UseFont = true;
        captionLabel.Appearance.Options.UseForeColor = true;

        card.Controls.Add(valueLabel);
        card.Controls.Add(captionLabel);
        return card;
    }

    private XtraTabPage BuildGridPage(string title, GridControl grid, string featureOperation)
    {
        var page = new XtraTabPage { Text = title };

        var previewButton = new SimpleButton { Text = "Preview" };
        var printButton = new SimpleButton { Text = "Print" };
        var exportPdfButton = new SimpleButton { Text = "Export PDF" };
        var exportExcelButton = new SimpleButton { Text = "Export Excel" };

        previewButton.Click += (_, _) => grid.ShowPrintPreview();
        printButton.Click += (_, _) => grid.ShowRibbonPrintPreview();
        exportPdfButton.Click += (_, _) => ExportGrid(grid, "PDF files (*.pdf)|*.pdf", $"{featureOperation}.pdf", (g, path) => g.ExportToPdf(path));
        exportExcelButton.Click += (_, _) => ExportGrid(grid, "Excel files (*.xlsx)|*.xlsx", $"{featureOperation}.xlsx", (g, path) => g.ExportToXlsx(path));

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        toolbar.Controls.Add(previewButton);
        toolbar.Controls.Add(printButton);
        toolbar.Controls.Add(exportPdfButton);
        toolbar.Controls.Add(exportExcelButton);

        page.Controls.Add(grid);
        page.Controls.Add(toolbar);
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
