using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.QuickBooks;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.QuickBooks;

/// <summary>Display item model for QuickBooks synchronization log entries.</summary>
public sealed class QuickBooksSyncDisplayItem
{
    /// <summary>Primary mapping ID.</summary>
    public Guid MapId { get; set; }

    /// <summary>Local CBOS transaction identifier (OrderId, PaymentId, ShiftId).</summary>
    public Guid LocalEntityId { get; set; }

    /// <summary>Target entity type (Invoice, Payment, ShiftSummary).</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Document number or reference.</summary>
    public string DocNumber { get; set; } = string.Empty;

    /// <summary>Financial amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Synchronization lifecycle status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Remote QuickBooks transaction ID (TxnID).</summary>
    public string QuickBooksTxnId { get; set; } = string.Empty;

    /// <summary>Number of retry attempts.</summary>
    public int RetryCount { get; set; }

    /// <summary>Last recorded error message.</summary>
    public string LastError { get; set; } = string.Empty;

    /// <summary>Formatted creation timestamp.</summary>
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>Formatted synchronization timestamp.</summary>
    public string SyncedAt { get; set; } = string.Empty;

    /// <summary>Raw JSON request payload.</summary>
    public string RequestPayloadJson { get; set; } = string.Empty;

    /// <summary>Raw JSON/XML response payload.</summary>
    public string ResponsePayloadJson { get; set; } = string.Empty;
}

/// <summary>
/// Back-Office QuickBooks Synchronization &amp; Outbox Reconciliation Monitor.
/// Provides real-time visibility into pending, synchronized, and failed accounting entries,
/// manual retry triggers, payload inspection, and manager override escalation tools.
/// Certified High-DPI compatible (100% to 250% PerMonitorV2 scaling).
/// </summary>
[DesignerCategory("Code")]
public sealed class QuickBooksSyncLogControl : XtraUserControl
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly IOutboxProcessor? _outboxProcessor;
    private readonly IOutboxRepository? _outboxRepository;
    private readonly ICircuitBreakerRegistry? _circuitBreakerRegistry;
    private readonly ICurrentSession? _currentSession;
    private readonly EventHandler<int>? _onBatchCompleted;

    private readonly TableLayoutPanel _mainLayout = new();
    private readonly PanelControl _headerPanel = new();
    private readonly TableLayoutPanel _kpiCardLayout = new();
    private readonly PanelControl _toolbarPanel = new();
    private readonly GridControl _gridControl = new();
    private readonly GridView _gridView = new();
    private readonly List<QuickBooksSyncDisplayItem> _dataItems = new();

    // KPI Summary Labels
    private readonly LabelControl _kpiPendingVal = new();
    private readonly LabelControl _kpiSyncedVal = new();
    private readonly LabelControl _kpiFailedVal = new();
    private readonly LabelControl _kpiTotalAmountVal = new();

    // Action Buttons
    private readonly SimpleButton _btnRetry = new();
    private readonly SimpleButton _btnInspectPayload = new();
    private readonly SimpleButton _btnManagerOverride = new();
    private readonly SimpleButton _btnRefresh = new();
    private readonly ComboBoxEdit _cmbStatusFilter = new();

    private readonly System.Windows.Forms.Timer _refreshTimer = new();

    /// <summary>Parameterless constructor for Visual Studio Designer safety.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public QuickBooksSyncLogControl() : this(null, null, null, null, null)
    {
    }

    /// <summary>Runtime constructor resolving dependencies via DI.</summary>
    public QuickBooksSyncLogControl(
        IServiceScopeFactory? scopeFactory,
        IOutboxProcessor? outboxProcessor = null,
        IOutboxRepository? outboxRepository = null,
        ICircuitBreakerRegistry? circuitBreakerRegistry = null,
        ICurrentSession? currentSession = null)
    {
        _scopeFactory = scopeFactory;
        _outboxProcessor = outboxProcessor;
        _outboxRepository = outboxRepository;
        _circuitBreakerRegistry = circuitBreakerRegistry;
        _currentSession = currentSession;

        _onBatchCompleted = (_, _) =>
        {
            if (IsHandleCreated && !IsDisposed)
            {
                try { BeginInvoke(new Action(LoadDataAsync)); } catch { }
            }
        };

        if (_outboxProcessor != null && _onBatchCompleted != null)
        {
            _outboxProcessor.BatchCompleted += _onBatchCompleted;
        }

        InitializeLayout();

        if (!DesignModeHelper.IsInDesignMode)
        {
            _refreshTimer.Interval = 5000;
            _refreshTimer.Tick += (_, _) => LoadDataAsync();
            _refreshTimer.Start();

            LoadDataAsync();
        }
    }

    private void InitializeLayout()
    {
        SuspendLayout();
        AutoScaleMode = AutoScaleMode.None;
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 249, 250);

        _mainLayout.Dock = DockStyle.Fill;
        _mainLayout.ColumnCount = 1;
        _mainLayout.RowCount = 4;
        _mainLayout.ColumnStyles.Clear();
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _mainLayout.RowStyles.Clear();
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); // Header
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 85)); // KPIs
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // Toolbar
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Grid

        BuildHeaderPanel();
        BuildKpiCards();
        BuildToolbar();
        BuildGrid();

        _mainLayout.Controls.Add(_headerPanel, 0, 0);
        _mainLayout.Controls.Add(_kpiCardLayout, 0, 1);
        _mainLayout.Controls.Add(_toolbarPanel, 0, 2);
        _mainLayout.Controls.Add(_gridControl, 0, 3);

        Controls.Add(_mainLayout);
        ResumeLayout(false);
    }

    private void BuildHeaderPanel()
    {
        _headerPanel.Dock = DockStyle.Fill;
        _headerPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _headerPanel.BackColor = Color.FromArgb(33, 37, 41);
        _headerPanel.Padding = new Padding(16, 10, 16, 10);
        _headerPanel.MinimumSize = new Size(0, 56);
        _headerPanel.Height = 56;

        var titleLabel = new LabelControl
        {
            Text = "QuickBooks Accounting Synchronization",
            Appearance = { Font = new Font("Segoe UI", 12.0f, FontStyle.Bold), ForeColor = Color.White },
            Dock = DockStyle.Left,
            AutoSizeMode = LabelAutoSizeMode.Horizontal
        };

        var subtitleLabel = new LabelControl
        {
            Text = "Durable Reference Mapping | Idempotent TxnID Registry | Manager Override",
            Appearance = { Font = new Font("Segoe UI", 8.5f, FontStyle.Regular), ForeColor = Color.FromArgb(173, 181, 189) },
            Dock = DockStyle.Right,
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Padding = new Padding(0, 4, 0, 0)
        };

        _headerPanel.Controls.Add(titleLabel);
        _headerPanel.Controls.Add(subtitleLabel);
    }

    private void BuildKpiCards()
    {
        _kpiCardLayout.Dock = DockStyle.Fill;
        _kpiCardLayout.ColumnCount = 4;
        _kpiCardLayout.RowCount = 1;
        _kpiCardLayout.MinimumSize = new Size(0, 85);
        _kpiCardLayout.Height = 85;
        _kpiCardLayout.Padding = new Padding(8, 4, 8, 4);

        for (int i = 0; i < 4; i++)
        {
            _kpiCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        _kpiCardLayout.Controls.Add(CreateKpiCard("PENDING OUTBOX", _kpiPendingVal, Color.FromArgb(255, 193, 7)), 0, 0);
        _kpiCardLayout.Controls.Add(CreateKpiCard("SYNCHRONIZED", _kpiSyncedVal, Color.FromArgb(40, 167, 69)), 1, 0);
        _kpiCardLayout.Controls.Add(CreateKpiCard("FAILED / REVIEW", _kpiFailedVal, Color.FromArgb(220, 53, 69)), 2, 0);
        _kpiCardLayout.Controls.Add(CreateKpiCard("TOTAL SYNCHRONIZED", _kpiTotalAmountVal, Color.FromArgb(13, 110, 253)), 3, 0);
    }

    private PanelControl CreateKpiCard(string caption, LabelControl valueLabel, Color accentColor)
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple,
            BackColor = Color.White,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(4)
        };

        var lblCap = new LabelControl
        {
            Text = caption,
            Appearance = { Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(108, 117, 125) },
            Dock = DockStyle.Top,
            Padding = new Padding(2)
        };
        lblCap.Appearance.Options.UseFont = true;
        lblCap.Appearance.Options.UseForeColor = true;

        valueLabel.Text = "-";
        valueLabel.Appearance.Font = new Font("Segoe UI", 16.0f, FontStyle.Bold);
        valueLabel.Appearance.ForeColor = accentColor;
        valueLabel.Appearance.Options.UseFont = true;
        valueLabel.Appearance.Options.UseForeColor = true;
        valueLabel.Appearance.TextOptions.HAlignment = HorzAlignment.Near;
        valueLabel.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Padding = new Padding(2);

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(lblCap);
        return panel;
    }

    private void BuildToolbar()
    {
        _toolbarPanel.Dock = DockStyle.Fill;
        _toolbarPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        _toolbarPanel.MinimumSize = new Size(0, 48);
        _toolbarPanel.Height = 48;
        _toolbarPanel.Padding = new Padding(8, 6, 8, 6);

        var toolbarFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };

        _btnRetry.Text = "Retry Selected";
        _btnRetry.Appearance.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
        _btnRetry.Appearance.Options.UseFont = true;
        _btnRetry.Size = new Size(130, 34);
        _btnRetry.Margin = new Padding(0, 2, 8, 2);
        _btnRetry.Click += async (_, _) => await HandleRetryAsync();

        _btnInspectPayload.Text = "Inspect Payload";
        _btnInspectPayload.Appearance.Font = new Font("Segoe UI", 9.0f);
        _btnInspectPayload.Appearance.Options.UseFont = true;
        _btnInspectPayload.Size = new Size(130, 34);
        _btnInspectPayload.Margin = new Padding(0, 2, 8, 2);
        _btnInspectPayload.Click += (_, _) => HandleInspectPayload();

        _btnManagerOverride.Text = "Manager Override";
        _btnManagerOverride.Appearance.Font = new Font("Segoe UI", 9.0f, FontStyle.Bold);
        _btnManagerOverride.Appearance.ForeColor = Color.DarkRed;
        _btnManagerOverride.Appearance.Options.UseFont = true;
        _btnManagerOverride.Appearance.Options.UseForeColor = true;
        _btnManagerOverride.Size = new Size(145, 34);
        _btnManagerOverride.Margin = new Padding(0, 2, 8, 2);
        _btnManagerOverride.Click += async (_, _) => await HandleManagerOverrideAsync();

        _btnRefresh.Text = "Refresh";
        _btnRefresh.Appearance.Font = new Font("Segoe UI", 9.0f);
        _btnRefresh.Appearance.Options.UseFont = true;
        _btnRefresh.Size = new Size(90, 34);
        _btnRefresh.Margin = new Padding(0, 2, 16, 2);
        _btnRefresh.Click += (_, _) => LoadDataAsync();

        var lblFilter = new LabelControl
        {
            Text = "Filter Status:",
            Appearance = { Font = new Font("Segoe UI", 9.0f, FontStyle.Bold) },
            Margin = new Padding(0, 8, 6, 2)
        };
        lblFilter.Appearance.Options.UseFont = true;

        _cmbStatusFilter.Properties.Items.Clear();
        _cmbStatusFilter.Properties.Items.AddRange(new object[] { "All", "Pending", "Synchronized", "Failed", "ManualReview" });
        _cmbStatusFilter.SelectedIndex = 0;
        _cmbStatusFilter.Properties.AutoHeight = false;
        _cmbStatusFilter.Size = new Size(140, 34);
        _cmbStatusFilter.Margin = new Padding(0, 2, 8, 2);
        _cmbStatusFilter.SelectedIndexChanged += (_, _) => LoadDataAsync();

        toolbarFlow.Controls.Add(_btnRetry);
        toolbarFlow.Controls.Add(_btnInspectPayload);
        toolbarFlow.Controls.Add(_btnManagerOverride);
        toolbarFlow.Controls.Add(_btnRefresh);
        toolbarFlow.Controls.Add(lblFilter);
        toolbarFlow.Controls.Add(_cmbStatusFilter);

        _toolbarPanel.Controls.Add(toolbarFlow);
    }

    private void BuildGrid()
    {
        _gridControl.Dock = DockStyle.Fill;
        _gridControl.MainView = _gridView;
        _gridControl.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { _gridView });

        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsSelection.EnableAppearanceFocusedCell = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ShowIndicator = false;
        _gridView.RowHeight = 28;

        AddGridColumn("EntityType", "Entity Type", 100);
        AddGridColumn("DocNumber", "Doc / Ref #", 110);
        AddGridColumn("Amount", "Amount", 90, HorzAlignment.Far, "N2");
        AddGridColumn("Status", "Status", 110);
        AddGridColumn("QuickBooksTxnId", "QuickBooks TxnID", 160);
        AddGridColumn("RetryCount", "Retries", 70, HorzAlignment.Center);
        AddGridColumn("LastError", "Last Error / Reason", 240);
        AddGridColumn("CreatedAt", "Created At (UTC)", 130);
        AddGridColumn("SyncedAt", "Synced At (UTC)", 130);

        _gridControl.DataSource = _dataItems;
    }

    private void AddGridColumn(string fieldName, string caption, int width, HorzAlignment align = HorzAlignment.Default, string? format = null)
    {
        var col = new GridColumn
        {
            FieldName = fieldName,
            Caption = caption,
            Visible = true,
            Width = width
        };
        col.AppearanceCell.TextOptions.HAlignment = align;
        col.AppearanceHeader.TextOptions.HAlignment = align;

        if (format != null)
        {
            col.DisplayFormat.FormatType = FormatType.Numeric;
            col.DisplayFormat.FormatString = format;
        }

        _gridView.Columns.Add(col);
    }

    /// <summary>Asynchronously reloads mapping records and outbox statistics from persistence.</summary>
    public async void LoadDataAsync()
    {
        if (_scopeFactory == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mapRepo = scope.ServiceProvider.GetService<IQuickBooksSyncMapRepository>();
            var outboxRepo = scope.ServiceProvider.GetService<IOutboxRepository>();

            if (mapRepo == null) return;

            QuickBooksSyncStatus? filter = null;
            var selectedFilter = _cmbStatusFilter.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(selectedFilter) && selectedFilter != "All" && Enum.TryParse<QuickBooksSyncStatus>(selectedFilter, out var parsed))
            {
                filter = parsed;
            }

            var maps = await mapRepo.GetRecentAsync(limit: 200, statusFilter: filter);

            var items = maps.Select(m => new QuickBooksSyncDisplayItem
            {
                MapId = m.Id,
                LocalEntityId = m.LocalEntityId,
                EntityType = m.EntityType,
                DocNumber = m.QuickBooksDocNumber ?? m.LocalEntityId.ToString()[..8].ToUpperInvariant(),
                Amount = m.Amount,
                Status = m.Status.ToString(),
                QuickBooksTxnId = m.QuickBooksTxnId ?? "-",
                RetryCount = m.RetryCount,
                LastError = m.LastError ?? (m.Status == QuickBooksSyncStatus.ManualReview ? m.ManagerOverrideNotes ?? "Manual Review" : "-"),
                CreatedAt = m.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                SyncedAt = m.SyncedAtUtc?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-",
                RequestPayloadJson = m.RequestPayloadJson ?? "{}",
                ResponsePayloadJson = m.ResponsePayloadJson ?? "{}"
            }).ToList();

            // Calculate KPIs
            var pendingCount = maps.Count(m => m.Status == QuickBooksSyncStatus.Pending);
            var syncedCount = maps.Count(m => m.Status == QuickBooksSyncStatus.Synchronized);
            var failedCount = maps.Count(m => m.Status == QuickBooksSyncStatus.Failed || m.Status == QuickBooksSyncStatus.ManualReview);
            var totalSyncedAmount = maps.Where(m => m.Status == QuickBooksSyncStatus.Synchronized).Sum(m => m.Amount);

            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(() =>
                {
                    _kpiPendingVal.Text = pendingCount.ToString();
                    _kpiSyncedVal.Text = syncedCount.ToString();
                    _kpiFailedVal.Text = failedCount.ToString();
                    _kpiTotalAmountVal.Text = totalSyncedAmount.ToString("C2");

                    _dataItems.Clear();
                    _dataItems.AddRange(items);
                    _gridView.RefreshData();
                }));
            }
        }
        catch
        {
            // Suppress background poll errors
        }
    }

    private QuickBooksSyncDisplayItem? GetSelectedItem()
    {
        var focusedRow = _gridView.GetFocusedRow();
        return focusedRow as QuickBooksSyncDisplayItem;
    }

    private async Task HandleRetryAsync()
    {
        var selected = GetSelectedItem();
        if (selected == null)
        {
            XtraMessageBox.Show("Please select an accounting sync record to retry.", "QuickBooks Reconciliation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_scopeFactory == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mapRepo = scope.ServiceProvider.GetService<IQuickBooksSyncMapRepository>();
            if (mapRepo != null)
            {
                var map = await mapRepo.GetByIdAsync(selected.MapId);
                if (map != null)
                {
                    map.ResetForRetry();
                    await mapRepo.UpdateAsync(map);
                }
            }

            // Signal outbox processor for immediate pickup
            _outboxProcessor?.TriggerImmediate();

            XtraMessageBox.Show($"Synchronization item for {selected.EntityType} ({selected.DocNumber}) reset to Pending and queued for immediate processing.",
                "QuickBooks Reconciliation", MessageBoxButtons.OK, MessageBoxIcon.Information);

            LoadDataAsync();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Failed to retry item: {ex.Message}", "Retry Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void HandleInspectPayload()
    {
        var selected = GetSelectedItem();
        if (selected == null)
        {
            XtraMessageBox.Show("Please select a record to inspect.", "QuickBooks Reconciliation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var details = $"ENTITY: {selected.EntityType}\n" +
                      $"DOC NUMBER: {selected.DocNumber}\n" +
                      $"AMOUNT: {selected.Amount:C2}\n" +
                      $"STATUS: {selected.Status}\n" +
                      $"QUICKBOOKS TXN ID: {selected.QuickBooksTxnId}\n" +
                      $"RETRIES: {selected.RetryCount}\n" +
                      $"LAST ERROR: {selected.LastError}\n\n" +
                      $"--- REQUEST PAYLOAD ---\n{selected.RequestPayloadJson}\n\n" +
                      $"--- RESPONSE / RECEIPT ---\n{selected.ResponsePayloadJson}";

        XtraMessageBox.Show(details, $"Payload Details: {selected.DocNumber}", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task HandleManagerOverrideAsync()
    {
        var selected = GetSelectedItem();
        if (selected == null)
        {
            XtraMessageBox.Show("Please select a failed sync record to override.", "Manager Override", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = XtraMessageBox.Show(
            $"Are you sure you want to manually mark {selected.EntityType} ({selected.DocNumber}) as resolved?\nThis marks the item under ManualReview and stops automated retry loops.",
            "Confirm Manager Override",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        if (_scopeFactory == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mapRepo = scope.ServiceProvider.GetService<IQuickBooksSyncMapRepository>();
            if (mapRepo != null)
            {
                var map = await mapRepo.GetByIdAsync(selected.MapId);
                if (map != null)
                {
                    var managerId = _currentSession?.UserId ?? Guid.Empty;
                    map.MarkManagerOverride(managerId, "Resolved by Store Manager via QuickBooks Reconciliation Dashboard.");
                    await mapRepo.UpdateAsync(map);
                }
            }

            XtraMessageBox.Show($"Item {selected.DocNumber} marked as ManualReview by Store Manager.", "Manager Override Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadDataAsync();
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show($"Failed to apply manager override: {ex.Message}", "Override Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();

            if (_outboxProcessor != null && _onBatchCompleted != null)
            {
                _outboxProcessor.BatchCompleted -= _onBatchCompleted;
            }

            _gridControl.Dispose();
            _gridView.Dispose();
            _headerPanel.Dispose();
            _toolbarPanel.Dispose();
            _kpiCardLayout.Dispose();
            _mainLayout.Dispose();
        }
        base.Dispose(disposing);
    }
}
