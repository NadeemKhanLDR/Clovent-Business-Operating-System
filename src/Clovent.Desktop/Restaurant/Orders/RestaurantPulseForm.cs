using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Restaurant.Application.RestaurantPulse.Dtos;
using Clovent.Restaurant.Application.RestaurantPulse.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// The Restaurant Pulse dashboard: today's headline sales figures, best
/// sellers, service speed and beverage upsell opportunity as a compact
/// read-only tile layout. All aggregation happens in the backend query -
/// this form only renders <see cref="RestaurantPulseDto"/>. Metrics the
/// backend could not compute render an explicit "Not available" state
/// instead of a misleading zero.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed class RestaurantPulseForm : XtraForm
{
    private const string NotAvailable = "Not available";

    private static readonly Color PageBack = Color.FromArgb(241, 245, 249);
    private static readonly Color TileBack = Color.White;
    private static readonly Color TileBorder = Color.FromArgb(226, 232, 240);
    private static readonly Color Accent = Color.FromArgb(13, 148, 136);
    private static readonly Color Muted = Color.FromArgb(100, 116, 139);
    private static readonly Color Dark = Color.FromArgb(15, 23, 42);

    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    private LabelControl _heroLabel = null!;
    private LabelControl _heroSubLabel = null!;
    private TableLayoutPanel _tiles = null!;
    private LabelControl _statusLabel = null!;

    /// <summary>Design-time-only constructor - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RestaurantPulseForm()
    {
        _mediator = null!;
        _logger = null!;
        InitializeComponent();
    }

    public RestaurantPulseForm(IMediator mediator, ILogger logger)
    {
        _mediator = mediator;
        _logger = logger;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Restaurant Pulse";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(880, 560);
        MinimizeBox = false;
        BackColor = PageBack;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            BackColor = PageBack
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));  // hero
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // tiles
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // status bar

        var hero = new DevExpress.XtraEditors.PanelControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(16, 16, 16, 8),
            Padding = new Padding(20, 14, 20, 14),
            BackColor = Dark
        };
        hero.Appearance.BackColor = Dark;
        hero.Appearance.Options.UseBackColor = true;
        hero.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

        _heroLabel = new LabelControl
        {
            Text = "Loading today's pulse…",
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.Vertical
        };
        _heroLabel.Appearance.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        _heroLabel.Appearance.ForeColor = Color.White;
        _heroLabel.Appearance.Options.UseFont = true;
        _heroLabel.Appearance.Options.UseForeColor = true;

        _heroSubLabel = new LabelControl
        {
            Text = string.Empty,
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.Vertical
        };
        _heroSubLabel.Appearance.Font = new Font("Segoe UI", 10F);
        _heroSubLabel.Appearance.ForeColor = Color.FromArgb(203, 213, 225);
        _heroSubLabel.Appearance.Options.UseFont = true;
        _heroSubLabel.Appearance.Options.UseForeColor = true;
        _heroSubLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _heroSubLabel.Appearance.Options.UseTextOptions = true;

        hero.Controls.Add(_heroSubLabel);
        hero.Controls.Add(_heroLabel);

        _tiles = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            Margin = new Padding(12, 4, 12, 4),
            BackColor = PageBack
        };
        for (int c = 0; c < 3; c++)
        {
            _tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        }
        for (int r = 0; r < 3; r++)
        {
            _tiles.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
        }

        _statusLabel = new LabelControl
        {
            Text = "Restaurant Pulse refreshes each time it is opened.",
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.Vertical,
            Padding = new Padding(16, 0, 0, 0)
        };
        _statusLabel.Appearance.Font = new Font("Segoe UI", 8.5F);
        _statusLabel.Appearance.ForeColor = Muted;
        _statusLabel.Appearance.Options.UseFont = true;
        _statusLabel.Appearance.Options.UseForeColor = true;
        _statusLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        _statusLabel.Appearance.Options.UseTextOptions = true;

        var refreshButton = new SimpleButton
        {
            Text = "Refresh",
            Dock = DockStyle.Right,
            Width = 100,
            Margin = new Padding(8, 6, 16, 6),
            Cursor = Cursors.Hand
        };
        refreshButton.Click += async (_, _) => await LoadPulseAsync();

        var statusPanel = new Panel { Dock = DockStyle.Fill, BackColor = PageBack, Margin = new Padding(0) };
        statusPanel.Controls.Add(_statusLabel);
        statusPanel.Controls.Add(refreshButton);

        root.Controls.Add(hero, 0, 0);
        root.Controls.Add(_tiles, 0, 1);
        root.Controls.Add(statusPanel, 0, 2);
        Controls.Add(root);

        Load += async (_, _) => await LoadPulseAsync();
    }

    private async Task LoadPulseAsync()
    {
        UseWaitCursor = true;
        _statusLabel.Text = "Loading…";
        try
        {
            var pulse = await _mediator.Send(new GetRestaurantPulseQuery());
            Render(pulse);
            _statusLabel.Text = $"Loaded {DateTime.Now:HH:mm}. Figures are computed from today's completed orders (UTC day).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Restaurant Pulse");
            _heroLabel.Text = "Restaurant Pulse unavailable";
            _heroSubLabel.Text = "Could not load today's figures. Please try again.";
            _statusLabel.Text = "Load failed.";
            XtraMessageBox.Show(this, "Unable to load the Restaurant Pulse data. Please try again.", "Restaurant Pulse", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void Render(RestaurantPulseDto pulse)
    {
        var salesVsYesterday = pulse.SalesVsYesterdayPercent is { } pct
            ? $"{(pct >= 0 ? "▲ +" : "▼ ")}{pct:0.0}% vs yesterday"
            : $"{NotAvailable} (no sales yesterday)";

        _heroLabel.Text = $"Today: {CurrencyDisplay.FormatPlain(pulse.TodaySales)}";
        _heroSubLabel.Text =
            $"{pulse.TodayOrderCount} orders  •  Avg order {CurrencyDisplay.FormatPlain(pulse.AverageOrderValue)}  •  {salesVsYesterday}  •  Yesterday {CurrencyDisplay.FormatPlain(pulse.YesterdaySales)}";

        _tiles.SuspendLayout();
        _tiles.Controls.Clear();

        AddTile(0, 0, "BEST SELLER", pulse.BestSellerName is { } name ? name : NotAvailable,
            pulse.BestSellerName is null ? null : $"{pulse.BestSellerQuantity:0.##} sold today");

        AddTile(1, 0, "HIGHEST REVENUE", pulse.HighestRevenueProductName is { } top ? top : NotAvailable,
            pulse.HighestRevenueProductName is null ? null : CurrencyDisplay.FormatPlain(pulse.HighestRevenueAmount));

        AddTile(2, 0, "AVG ORDER TIME", pulse.AverageOrderTimeSeconds is { } secs
            ? $"{TimeSpan.FromSeconds(secs):hh\\:mm\\:ss}"
            : NotAvailable,
            pulse.AverageOrderTimeSeconds is null ? "No completed orders today" : "From open to completion");

        AddTile(0, 1, "REVENUE OPPORTUNITY",
            pulse.BeverageInfoAvailable ? CurrencyDisplay.FormatPlain(pulse.BeverageAddOnRevenueOpportunity ?? 0m) : NotAvailable,
            pulse.BeverageInfoAvailable
                ? $"{pulse.OrdersWithoutBeverageCount} orders without a beverage"
                : "No beverage category configured");

        AddTile(1, 1, "LOW STOCK", pulse.InventoryAvailable ? FormatLowStock(pulse) : "No inventory tracking configured",
            pulse.InventoryAvailable ? null : "Connect the Inventory module to see low-stock items");

        AddTile(2, 1, "ORDERS TODAY", pulse.TodayOrderCount.ToString("N0"), "Completed sales");

        _tiles.ResumeLayout(true);
    }

    private static string FormatLowStock(RestaurantPulseDto pulse)
    {
        if (pulse.LowStockItems.Count == 0)
        {
            return "All stocked";
        }

        return string.Join(Environment.NewLine, pulse.LowStockItems.Take(3).Select(i => $"{i.ProductName}: {i.QuantityOnHand:0.##}"));
    }

    private void AddTile(int column, int row, string caption, string value, string? footnote)
    {
        var tile = new DevExpress.XtraEditors.PanelControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            Padding = new Padding(14, 10, 14, 10),
            BackColor = TileBack
        };
        tile.Appearance.BackColor = TileBack;
        tile.Appearance.Options.UseBackColor = true;
        tile.Appearance.BorderColor = TileBorder;
        tile.Appearance.Options.UseBorderColor = true;
        tile.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;

        var lblCaption = new LabelControl
        {
            Text = caption,
            Dock = DockStyle.Top,
            AutoSizeMode = LabelAutoSizeMode.Vertical
        };
        lblCaption.Appearance.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        lblCaption.Appearance.ForeColor = Muted;
        lblCaption.Appearance.Options.UseFont = true;
        lblCaption.Appearance.Options.UseForeColor = true;

        var lblFootnote = new LabelControl
        {
            Text = footnote ?? string.Empty,
            Dock = DockStyle.Bottom,
            AutoSizeMode = LabelAutoSizeMode.Vertical,
            Visible = footnote is not null
        };
        lblFootnote.Appearance.Font = new Font("Segoe UI", 8.5F);
        lblFootnote.Appearance.ForeColor = Muted;
        lblFootnote.Appearance.Options.UseFont = true;
        lblFootnote.Appearance.Options.UseForeColor = true;

        var lblValue = new LabelControl
        {
            Text = value,
            Dock = DockStyle.Fill,
            AutoSizeMode = LabelAutoSizeMode.Vertical
        };
        lblValue.Appearance.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblValue.Appearance.ForeColor = Accent;
        lblValue.Appearance.Options.UseFont = true;
        lblValue.Appearance.Options.UseForeColor = true;
        lblValue.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
        lblValue.Appearance.Options.UseTextOptions = true;

        tile.Controls.Add(lblValue);
        tile.Controls.Add(lblFootnote);
        tile.Controls.Add(lblCaption);

        _tiles.Controls.Add(tile, column, row);
    }
}
