using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Restaurant.Application.CustomerReorder.Dtos;
using Clovent.Restaurant.Application.CustomerReorder.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Lists a customer's most recent completed order, resolved against today's
/// catalog: unavailable items are shown disabled with an "Unavailable" note
/// and are excluded when the cashier confirms "Repeat". The caller replays
/// the selected lines through the ordinary add-to-order path.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class CustomerReorderDialog : XtraForm
{
    private static readonly Color UnavailableFore = Color.FromArgb(148, 163, 184);

    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly Guid _customerId;

    private DevExpress.XtraGrid.GridControl _grid = null!;
    private DevExpress.XtraGrid.Views.Grid.GridView _view = null!;
    private LabelControl _headerLabel = null!;
    private SimpleButton _repeatButton = null!;
    private IReadOnlyList<CustomerReorderLineDto> _availableLines = [];

    /// <summary>The available lines to replay when the dialog was confirmed.</summary>
    public IReadOnlyCollection<CustomerReorderLineDto> SelectedLines { get; private set; } = [];

    /// <summary>How many lines were unavailable and therefore excluded.</summary>
    public int UnavailableCount { get; private set; }

    /// <summary>Design-time-only constructor - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CustomerReorderDialog()
    {
        _mediator = null!;
        _logger = null!;
        _customerId = Guid.Empty;
        InitializeComponent();
    }

    public CustomerReorderDialog(IMediator mediator, ILogger logger, Guid customerId)
    {
        _mediator = mediator;
        _logger = logger;
        _customerId = customerId;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Repeat Last Order";
        DesktopDialogSizing.Apply(this, 620, 500, 540, 440, null, true);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(52, this)));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(50, this)));

        _headerLabel = new LabelControl
        {
            Text = "Loading the customer's last order…",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.Vertical,
            Padding = new Padding(DesktopDpi.Scale(16, this), DesktopDpi.Scale(12, this), DesktopDpi.Scale(16, this), 0)
        };
        _headerLabel.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _headerLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _headerLabel.Appearance.Options.UseFont = true;
        _headerLabel.Appearance.Options.UseForeColor = true;
        _headerLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _headerLabel.Appearance.Options.UseTextOptions = true;

        _grid = new DevExpress.XtraGrid.GridControl { Dock = DockStyle.Fill, Margin = new Padding(DesktopDpi.Scale(12, this), DesktopDpi.Scale(4, this), DesktopDpi.Scale(12, this), DesktopDpi.Scale(4, this)) };
        _view = new DevExpress.XtraGrid.Views.Grid.GridView
        {
            RowHeight = DesktopDpi.Scale(30, this)
        };
        _view.OptionsBehavior.Editable = false;
        _view.OptionsView.ShowGroupPanel = false;
        _view.OptionsView.ShowIndicator = false;
        _grid.MainView = _view;
        _grid.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { _view });

        _view.Columns.AddRange(
        [
            new DevExpress.XtraGrid.Columns.GridColumn { FieldName = nameof(ReorderRow.Item), Caption = "Item", Visible = true, Width = DesktopDpi.Scale(200, this) },
            new DevExpress.XtraGrid.Columns.GridColumn { FieldName = nameof(ReorderRow.Qty), Caption = "Qty", Visible = true, Width = DesktopDpi.Scale(50, this) },
            new DevExpress.XtraGrid.Columns.GridColumn { FieldName = nameof(ReorderRow.Price), Caption = "Today's Price", Visible = true, Width = DesktopDpi.Scale(100, this) },
            new DevExpress.XtraGrid.Columns.GridColumn { FieldName = nameof(ReorderRow.Note), Caption = "Note", Visible = true, Width = DesktopDpi.Scale(110, this) },
        ]);

        var footer = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = Color.White };

        _repeatButton = new SimpleButton
        {
            Text = "Repeat Order",
            Dock = DockStyle.Right,
            Width = DesktopDpi.Scale(140, this),
            Margin = new Padding(DesktopDpi.Scale(8, this)),
            Enabled = false,
            Cursor = Cursors.Hand
        };
        _repeatButton.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _repeatButton.Appearance.BackColor = Color.FromArgb(13, 148, 136);
        _repeatButton.Appearance.ForeColor = Color.White;
        _repeatButton.Appearance.Options.UseFont = true;
        _repeatButton.Appearance.Options.UseBackColor = true;
        _repeatButton.Appearance.Options.UseForeColor = true;
        _repeatButton.Click += (_, _) =>
        {
            SelectedLines = _availableLines;
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancelButton = new SimpleButton
        {
            Text = "Cancel",
            Dock = DockStyle.Right,
            Width = DesktopDpi.Scale(95, this),
            Margin = new Padding(DesktopDpi.Scale(8, this)),
            Cursor = Cursors.Hand
        };
        cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footer.Controls.Add(_repeatButton);
        footer.Controls.Add(cancelButton);

        root.Controls.Add(_headerLabel, 0, 0);
        root.Controls.Add(_grid, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        Load += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var lastOrder = await _mediator.Send(new GetCustomerLastOrderQuery(_customerId));
            if (lastOrder is null)
            {
                _headerLabel.Text = "This customer has no previous completed order.";
                _grid.DataSource = null;
                return;
            }

            var localDate = DateTimeDisplay.Format(lastOrder.OrderDateUtc);
            var unavailable = lastOrder.Lines.Count(l => !l.IsAvailable);
            _headerLabel.Text = $"Last order {lastOrder.OrderNumber} • {localDate}"
                + (unavailable > 0 ? $" • {unavailable} item(s) unavailable" : string.Empty);

            _availableLines = lastOrder.Lines.Where(l => l.IsAvailable).ToList();
            _grid.DataSource = lastOrder.Lines.Select(l => new ReorderRow(l)).ToList();
            _repeatButton.Enabled = _availableLines.Count > 0;
            UnavailableCount = lastOrder.Lines.Count - _availableLines.Count;

            // Gray out unavailable rows.
            _view.RowStyle += (_, e) =>
            {
                if (_view.GetRow(e.RowHandle) is ReorderRow { IsAvailable: false })
                {
                    e.Appearance.ForeColor = UnavailableFore;
                    e.Appearance.Options.UseForeColor = true;
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load last order for customer {CustomerId}", _customerId);
            _headerLabel.Text = "Could not load the customer's last order.";
            XtraMessageBox.Show(this, "Unable to load the customer's last order. Please try again.", "Repeat Last Order", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private sealed class ReorderRow
    {
        public ReorderRow(CustomerReorderLineDto line)
        {
            Line = line;
            Item = line.ProductName.Equals(line.VariantName, StringComparison.OrdinalIgnoreCase)
                ? line.ProductName
                : $"{line.ProductName} - {line.VariantName}";
            Qty = line.Quantity.ToString("0.##");
            Price = CurrencyDisplay.FormatPlain(line.UnitPrice);
            Note = line.IsAvailable ? string.Empty : "Unavailable";
        }

        public CustomerReorderLineDto Line { get; }
        public string Item { get; }
        public string Qty { get; }
        public string Price { get; }
        public string Note { get; }
        public bool IsAvailable => Line.IsAvailable;
    }
}

/// <summary>
/// Read-only "Customer Order Insights" view: the customer's most frequently
/// ordered items with how many of their orders each appeared in.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class CustomerInsightsDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly Guid _customerId;

    private LabelControl _headerLabel = null!;
    private DevExpress.XtraGrid.GridControl _grid = null!;

    /// <summary>Design-time-only constructor - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public CustomerInsightsDialog()
    {
        _mediator = null!;
        _logger = null!;
        _customerId = Guid.Empty;
        InitializeComponent();
    }

    public CustomerInsightsDialog(IMediator mediator, ILogger logger, Guid customerId)
    {
        _mediator = mediator;
        _logger = logger;
        _customerId = customerId;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Customer Order Insights";
        DesktopDialogSizing.Apply(this, 580, 460, 500, 380, null, true);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(48, this)));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _headerLabel = new LabelControl
        {
            Text = "Loading…",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.Vertical,
            Padding = new Padding(DesktopDpi.Scale(16, this), DesktopDpi.Scale(12, this), DesktopDpi.Scale(16, this), 0)
        };
        _headerLabel.Appearance.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _headerLabel.Appearance.ForeColor = Color.FromArgb(15, 23, 42);
        _headerLabel.Appearance.Options.UseFont = true;
        _headerLabel.Appearance.Options.UseForeColor = true;
        _headerLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _headerLabel.Appearance.Options.UseTextOptions = true;

        _grid = new DevExpress.XtraGrid.GridControl { Dock = DockStyle.Fill, Margin = new Padding(DesktopDpi.Scale(12, this), DesktopDpi.Scale(4, this), DesktopDpi.Scale(12, this), DesktopDpi.Scale(12, this)) };
        var view = new DevExpress.XtraGrid.Views.Grid.GridView
        {
            RowHeight = DesktopDpi.Scale(30, this)
        };
        view.OptionsBehavior.Editable = false;
        view.OptionsView.ShowGroupPanel = false;
        view.OptionsView.ShowIndicator = false;
        _grid.MainView = view;
        _grid.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { view });
        view.Columns.AddRange(
        [
            new DevExpress.XtraGrid.Columns.GridColumn { FieldName = "Item", Caption = "Frequently Ordered Item", Visible = true, Width = DesktopDpi.Scale(240, this) },
            new DevExpress.XtraGrid.Columns.GridColumn { FieldName = "Times", Caption = "Orders", Visible = true, Width = DesktopDpi.Scale(70, this) },
            new DevExpress.XtraGrid.Columns.GridColumn { FieldName = "Price", Caption = "Today's Price", Visible = true, Width = DesktopDpi.Scale(100, this) },
        ]);

        var closeButton = new SimpleButton
        {
            Text = "Close",
            Dock = DockStyle.Right,
            Width = DesktopDpi.Scale(95, this),
            Margin = new Padding(DesktopDpi.Scale(8, this)),
            Cursor = Cursors.Hand
        };
        closeButton.Click += (_, _) => Close();

        var footer = new Panel { Dock = DockStyle.Bottom, Height = DesktopDpi.Scale(44, this), BackColor = Color.White };
        footer.Controls.Add(closeButton);

        root.Controls.Add(_headerLabel, 0, 0);
        root.Controls.Add(_grid, 0, 1);
        Controls.Add(root);
        Controls.Add(footer);

        Load += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var frequent = await _mediator.Send(new GetCustomerFrequentProductsQuery(_customerId, 10));
            _headerLabel.Text = frequent.Count == 0
                ? "No order history for this customer yet."
                : "This customer's most frequently ordered items:";

            _grid.DataSource = frequent.Select(l => new
            {
                Item = l.ProductName.Equals(l.VariantName, StringComparison.OrdinalIgnoreCase)
                    ? l.ProductName
                    : $"{l.ProductName} - {l.VariantName}",
                Times = l.Quantity.ToString("N0"),
                Price = CurrencyDisplay.FormatPlain(l.UnitPrice)
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load order insights for customer {CustomerId}", _customerId);
            _headerLabel.Text = "Could not load the customer's order insights.";
        }
    }
}
