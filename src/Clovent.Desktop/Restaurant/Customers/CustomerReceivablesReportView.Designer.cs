using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant.Customers;

partial class CustomerReceivablesReportView
{
    private System.ComponentModel.IContainer components = null;

    private readonly LabelControl _titleLabel = new() { Text = "Customer Receivables / A/R Aging" };
    private readonly LabelControl _subtitleLabel = new() { Text = "Customer outstanding balances, credit limits, advance balances, and aging distribution" };

    private readonly DateEdit _asOfDateEdit = new() { EditValue = DateTime.Today };
    private readonly ComboBoxEdit _filterCombo = new();
    private readonly TextEdit _searchEdit = new();

    private readonly SimpleButton _refreshButton = new() { Text = "Refresh" };
    private readonly SimpleButton _btnReceivePayment = new() { Text = "Receive Payment" };
    private readonly SimpleButton _btnBulkReceive = new() { Text = "Bulk Receive" };
    private readonly SimpleButton _btnLedger = new() { Text = "View Ledger" };
    private readonly SimpleButton _btnStatement = new() { Text = "Customer Statement" };
    private readonly SimpleButton _previewButton = new() { Text = "Preview" };
    private readonly SimpleButton _printButton = new() { Text = "Print" };
    private readonly SimpleButton _exportPdfButton = new() { Text = "Export PDF" };
    private readonly SimpleButton _exportExcelButton = new() { Text = "Export Excel" };

    private readonly LabelControl _totalReceivablesLabel = new();
    private readonly LabelControl _totalAdvancesLabel = new();
    private readonly LabelControl _totalOverLimitLabel = new();
    private readonly LabelControl _accountsWithBalanceLabel = new();

    private readonly GridControl _grid = new() { Dock = DockStyle.Fill };
    private readonly GridView _gridView = new();
    private TableLayoutPanel _row1Panel = null!;

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "CustomerReceivablesReportView";

        BuildGrid();

        // Title bar
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
            Padding = new Padding(16, 14, 16, 6),
        };
        titleBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titleBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleBar.Controls.Add(_titleLabel, 0, 0);
        titleBar.Controls.Add(_subtitleLabel, 0, 1);

        // Filter toolbar (Row 1)
        var asOfLabel = new LabelControl { Text = "As of Date:", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        asOfLabel.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        asOfLabel.Appearance.ForeColor = Color.FromArgb(51, 65, 85);
        asOfLabel.Appearance.Options.UseFont = true;
        asOfLabel.Appearance.Options.UseForeColor = true;
        asOfLabel.Padding = new Padding(0, 6, 0, 0);

        var filterLabel = new LabelControl { Text = "Filter:", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        filterLabel.Appearance.ForeColor = Color.Gray;
        filterLabel.Appearance.Options.UseForeColor = true;
        filterLabel.Padding = new Padding(0, 6, 0, 0);

        _asOfDateEdit.Width = 150;
        _asOfDateEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _asOfDateEdit.Properties.Appearance.Options.UseFont = true;
        _asOfDateEdit.Properties.DisplayFormat.FormatString = "dd-MMM-yyyy";
        _asOfDateEdit.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        _asOfDateEdit.Properties.EditFormat.FormatString = "dd-MMM-yyyy";
        _asOfDateEdit.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        _asOfDateEdit.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.DateTimeMaskManager));
        _asOfDateEdit.Properties.MaskSettings.Set("mask", "dd-MMM-yyyy");

        _filterCombo.Width = 190;
        _filterCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _filterCombo.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _filterCombo.Properties.Appearance.Options.UseFont = true;
        _filterCombo.Properties.AppearanceDropDown.Font = new Font("Segoe UI", 9.5F);
        _filterCombo.Properties.Items.AddRange(new object[] { "All Customers", "Has Balance Only", "Over Limit Only", "Holding Advance" });
        _filterCombo.SelectedIndex = 1; // Default to "Has Balance Only"

        var searchLabel = new LabelControl { Text = "Search:", AutoSizeMode = LabelAutoSizeMode.Horizontal };
        searchLabel.Appearance.ForeColor = Color.Gray;
        searchLabel.Appearance.Options.UseForeColor = true;
        searchLabel.Padding = new Padding(0, 6, 0, 0);

        _searchEdit.Width = 360;
        _searchEdit.Properties.NullValuePrompt = "Search code, name, mobile or phone...";
        _searchEdit.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _searchEdit.Properties.Appearance.Options.UseFont = true;

        var allButtons = new[]
        {
            _refreshButton, _btnReceivePayment, _btnBulkReceive, _btnLedger, _btnStatement,
            _previewButton, _printButton, _exportPdfButton, _exportExcelButton
        };

        foreach (var button in allButtons)
        {
            button.AutoSize = true;
            button.MinimumSize = new Size(0, 32);
            button.Padding = new Padding(10, 4, 10, 4);
            button.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Appearance.Options.UseFont = true;
            button.Cursor = Cursors.Hand;
        }

        _refreshButton.MinimumSize = new Size(90, 32);
        _refreshButton.Appearance.BackColor = Color.FromArgb(13, 148, 136); // Teal-600
        _refreshButton.Appearance.ForeColor = Color.White;
        _refreshButton.Appearance.Options.UseBackColor = true;
        _refreshButton.Appearance.Options.UseForeColor = true;

        _btnReceivePayment.Appearance.BackColor = Color.FromArgb(30, 64, 175); // Blue-700
        _btnReceivePayment.Appearance.ForeColor = Color.White;
        _btnReceivePayment.Appearance.Options.UseBackColor = true;
        _btnReceivePayment.Appearance.Options.UseForeColor = true;

        _btnBulkReceive.Appearance.BackColor = Color.FromArgb(3, 105, 161); // Sky-700
        _btnBulkReceive.Appearance.ForeColor = Color.White;
        _btnBulkReceive.Appearance.Options.UseBackColor = true;
        _btnBulkReceive.Appearance.Options.UseForeColor = true;

        _row1Panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 7,
            RowCount = 1,
            Padding = new Padding(16, 2, 16, 2),
        };
        _row1Panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _row1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _row1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(150, this)));
        _row1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _row1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DesktopDpi.Scale(180, this)));
        _row1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _row1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _row1Panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        asOfLabel.Margin = new Padding(0, 6, 8, 4);
        _asOfDateEdit.Margin = new Padding(0, 3, 14, 3);
        _asOfDateEdit.Dock = DockStyle.Fill;

        filterLabel.Margin = new Padding(0, 6, 8, 4);
        _filterCombo.Margin = new Padding(0, 3, 14, 3);
        _filterCombo.Dock = DockStyle.Fill;

        searchLabel.Margin = new Padding(0, 6, 8, 4);
        _searchEdit.Margin = new Padding(0, 3, 12, 3);
        _searchEdit.Dock = DockStyle.Fill;

        _refreshButton.Margin = new Padding(0, 3, 0, 3);
        _refreshButton.Dock = DockStyle.Fill;

        _row1Panel.Controls.Add(asOfLabel, 0, 0);
        _row1Panel.Controls.Add(_asOfDateEdit, 1, 0);
        _row1Panel.Controls.Add(filterLabel, 2, 0);
        _row1Panel.Controls.Add(_filterCombo, 3, 0);
        _row1Panel.Controls.Add(searchLabel, 4, 0);
        _row1Panel.Controls.Add(_searchEdit, 5, 0);
        _row1Panel.Controls.Add(_refreshButton, 6, 0);

        var row2Panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(16, 2, 16, 4),
        };

        void AddControlR2(Control c)
        {
            c.Margin = new Padding(4, 3, 6, 3);
            row2Panel.Controls.Add(c);
        }

        AddControlR2(_btnReceivePayment);
        AddControlR2(_btnBulkReceive);
        AddControlR2(_btnLedger);
        AddControlR2(_btnStatement);
        AddControlR2(_previewButton);
        AddControlR2(_printButton);
        AddControlR2(_exportPdfButton);
        AddControlR2(_exportExcelButton);

        var cardsRow = BuildCardsRow();
        cardsRow.Dock = DockStyle.Fill;

        var headerContainer = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 4
        };
        headerContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        headerContainer.Controls.Add(titleBar, 0, 0);
        headerContainer.Controls.Add(_row1Panel, 0, 1);
        headerContainer.Controls.Add(row2Panel, 0, 2);
        headerContainer.Controls.Add(cardsRow, 0, 3);

        Controls.Add(_grid);
        Controls.Add(headerContainer);

        _asOfDateEdit.EditValueChanged += (_, _) => RefreshReport();
        _filterCombo.SelectedIndexChanged += (_, _) => RefreshReport();
        _searchEdit.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) RefreshReport(); };
        _refreshButton.Click += (_, _) => RefreshReport();
        _previewButton.Click += PreviewButton_Click;
        _printButton.Click += PrintButton_Click;
        _exportPdfButton.Click += ExportPdfButton_Click;
        _exportExcelButton.Click += ExportExcelButton_Click;
        _gridView.DoubleClick += GridView_DoubleClick;

        Load += CustomerReceivablesReportView_Load;
    }

    private TableLayoutPanel BuildCardsRow()
    {
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
            Padding = new Padding(16, 6, 16, 6),
        };
        cardsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        for (var i = 0; i < 4; i++)
        {
            cardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        }

        cardsRow.Controls.Add(BuildStatCard("TOTAL RECEIVABLES (A/R)", _totalReceivablesLabel, Color.FromArgb(41, 128, 185), "Outstanding Customer Debts", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 0, 0);
        cardsRow.Controls.Add(BuildStatCard("TOTAL ADVANCES", _totalAdvancesLabel, Color.FromArgb(30, 64, 175), "Unapplied Customer Credit", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 1, 0);
        cardsRow.Controls.Add(BuildStatCard("TOTAL OVER LIMIT", _totalOverLimitLabel, Color.FromArgb(192, 57, 43), "Balances Exceeding Limit", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 2, 0);
        cardsRow.Controls.Add(BuildStatCard("ACCOUNTS W/ BALANCE", _accountsWithBalanceLabel, Color.FromArgb(39, 174, 96), "Active Credit Accounts", captionFont, valueFont, subFont, captionHeight, valueHeight, subHeight), 3, 0);

        return cardsRow;
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

    private void BuildGrid()
    {
        _grid.MainView = _gridView;
        _grid.ViewCollection.Add(_gridView);
        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsSelection.MultiSelect = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.EnableAppearanceEvenRow = true;
        _gridView.OptionsView.ColumnAutoWidth = true;
        _gridView.OptionsView.ShowFooter = true;
        _gridView.RowHeight = 28;

        AddGridColumn("CustomerCode", "Code", 75, false);
        AddGridColumn("CustomerName", "Customer Name", 160, false);
        AddGridColumn("MobileNumber", "Mobile", 95, false);
        AddGridColumn("CreditLimit", "Credit Limit", 95, true);
        AddGridColumn("Receivable", "Receivable (A/R)", 105, true);
        AddGridColumn("Advance", "Advance", 95, true);
        AddGridColumn("CurrentBalance", "Net Balance", 100, true);
        AddGridColumn("AvailableCredit", "Avail. Credit", 95, true);
        AddGridColumn("LastTransactionDate", "Last Activity", 115, false);
        AddGridColumn("CurrentBucket", "Current (0d)", 85, true);
        AddGridColumn("Days1To7Bucket", "1–7 Days", 85, true);
        AddGridColumn("Days8To15Bucket", "8–15 Days", 85, true);
        AddGridColumn("Days16To30Bucket", "16–30 Days", 85, true);
        AddGridColumn("Days31To60Bucket", "31–60 Days", 85, true);
        AddGridColumn("Days60PlusBucket", "60+ Days", 85, true);
        AddGridColumn("Status", "Status", 70, false);

        // Configure summaries on footers
        _gridView.Columns["CustomerName"].SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Custom;
        _gridView.Columns["CustomerName"].SummaryItem.DisplayFormat = "Total";

        string[] sumColumns = ["Receivable", "Advance", "CurrentBalance", "CurrentBucket", "Days1To7Bucket", "Days8To15Bucket", "Days16To30Bucket", "Days31To60Bucket", "Days60PlusBucket"];
        foreach (var sumCol in sumColumns)
        {
            if (_gridView.Columns[sumCol] is { } c)
            {
                c.SummaryItem.SummaryType = DevExpress.Data.SummaryItemType.Sum;
                c.SummaryItem.DisplayFormat = "{0:n2}";
            }
        }

        _gridView.CustomColumnDisplayText += GridView_CustomColumnDisplayText;
        _gridView.RowStyle += GridView_RowStyle;
        _gridView.CustomDrawEmptyForeground += GridView_CustomDrawEmptyForeground;
    }

    private void AddGridColumn(string fieldName, string caption, int width, bool isRight)
    {
        var col = _gridView.Columns.AddVisible(fieldName, caption);
        col.Width = width;
        if (isRight)
        {
            col.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            col.AppearanceCell.Options.UseTextOptions = true;
            col.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            col.AppearanceHeader.Options.UseTextOptions = true;
        }
    }

    public void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        int editorH = DesktopDpi.Scale(32, this);
        int asOfW = DesktopDpi.Scale(150, this);
        int filterW = DesktopDpi.Scale(180, this);
        int searchMinW = DesktopDpi.Scale(340, this);
        int refreshMinW = DesktopDpi.Scale(100, this);

        if (_row1Panel != null && _row1Panel.ColumnStyles.Count >= 7)
        {
            _row1Panel.ColumnStyles[1].Width = asOfW;
            _row1Panel.ColumnStyles[3].Width = filterW;
        }

        _asOfDateEdit.MinimumSize = new Size(asOfW, editorH);
        _filterCombo.MinimumSize = new Size(filterW, editorH);
        _searchEdit.MinimumSize = new Size(searchMinW, editorH);
        _refreshButton.MinimumSize = new Size(refreshMinW, editorH);

        foreach (var button in new[]
        {
            _btnReceivePayment, _btnBulkReceive, _btnLedger, _btnStatement,
            _previewButton, _printButton, _exportPdfButton, _exportExcelButton
        })
        {
            button.MinimumSize = new Size(DesktopDpi.Scale(90, this), editorH);
        }

        _gridView.RowHeight = DesktopDpi.Scale(28, this);
        _gridView.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);
    }
}
