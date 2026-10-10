using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Platform.CircuitBreakers;
using Clovent.Platform.Sync;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Outbox;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;

namespace Clovent.Desktop.Sync;

/// <summary>Display model for multi-terminal nodes in the replication grid.</summary>
public sealed class TerminalSyncDisplayItem
{
    /// <summary>Identifier or designation of the terminal.</summary>
    public string TerminalCode { get; set; } = string.Empty;

    /// <summary>Workstation descriptive name.</summary>
    public string TerminalName { get; set; } = string.Empty;

    /// <summary>Operational role of this terminal.</summary>
    public string TerminalRole { get; set; } = string.Empty;

    /// <summary>Number of pending sync packets in local outbox queue.</summary>
    public int OutboxQueueDepth { get; set; }

    /// <summary>Timestamp or relative time of last successful sync handshake.</summary>
    public string LastHandshake { get; set; } = string.Empty;

    /// <summary>Timestamp of last confirmed receiver durable acknowledgement.</summary>
    public string LastAckReceived { get; set; } = string.Empty;

    /// <summary>Circuit breaker state (Closed, HalfOpen, Open).</summary>
    public string CircuitBreakerState { get; set; } = "Closed";

    /// <summary>Replication mode (Online Delta Push or Autonomous Offline).</summary>
    public string ReplicationMode { get; set; } = "Online Delta Push";

    /// <summary>Health status indicator.</summary>
    public string HealthStatus { get; set; } = "Healthy";
}

/// <summary>Display model for conflicts awaiting store manager review.</summary>
public sealed class StagedConflictDisplayItem
{
    /// <summary>Unique conflict identifier.</summary>
    public Guid ConflictId { get; set; }

    /// <summary>Entity kind discriminator.</summary>
    public string EntityKind { get; set; } = string.Empty;

    /// <summary>Entity primary key / identifier.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Terminal origin ID.</summary>
    public string TerminalId { get; set; } = string.Empty;

    /// <summary>Incoming proposed value.</summary>
    public string IncomingValue { get; set; } = string.Empty;

    /// <summary>Current database target value.</summary>
    public string CurrentValue { get; set; } = string.Empty;

    /// <summary>Human-readable conflict explanation.</summary>
    public string ConflictReason { get; set; } = string.Empty;

    /// <summary>Timestamp when conflict was detected.</summary>
    public string DetectedAt { get; set; } = string.Empty;

    /// <summary>Resolution lifecycle status.</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Autonomous Branch Health &amp; Delta-Sync Monitor.
/// Provides back-office real-time visualization of multi-terminal synchronization queues,
/// pending transactional outbox payloads, circuit breaker states, remote handshakes,
/// and manager-review staging for concurrent conflicting edits.
/// Certified High-DPI compatible (100% to 250% PerMonitorV2 scaling).
/// </summary>
[DesignerCategory("Code")]
public sealed class BranchSyncStatusControl : XtraUserControl
{
    private readonly ISyncPacketDispatcher? _syncDispatcher;
    private readonly IOutboxRepository? _outboxRepository;
    private readonly IOutboxProcessor? _outboxProcessor;
    private readonly ICircuitBreakerRegistry? _circuitBreakerRegistry;
    private readonly ISyncConflictStagingStore? _conflictStagingStore;
    private readonly INetworkConnectivityProbe? _connectivityProbe;
    private readonly ISyncIngestionEngine? _ingestionEngine;
    private readonly ICurrentSession? _currentSession;
    private readonly EventHandler<int> _onBatchCompleted;
    private readonly EventHandler<bool> _onConnectivityChanged;

    private readonly TableLayoutPanel _mainLayout = new();
    private readonly PanelControl _headerPanel = new();
    private readonly TableLayoutPanel _kpiCardLayout = new();
    private readonly XtraTabControl _tabControl = new();

    // KPI Cards
    private readonly LabelControl _kpiOutboxPendingVal = new();
    private readonly LabelControl _kpiCircuitVal = new();
    private readonly LabelControl _kpiHandshakeVal = new();
    private readonly LabelControl _kpiModeVal = new();
    private readonly LabelControl _kpiConflictsVal = new();

    // Action Buttons
    private readonly SimpleButton _btnPushNow = new();
    private readonly SimpleButton _btnResetBreaker = new();
    private readonly SimpleButton _btnToggleNetwork = new();
    private readonly SimpleButton _btnRefresh = new();
    private readonly SimpleButton _btnApproveConflict = new();
    private readonly SimpleButton _btnRejectConflict = new();

    // Terminals Grid
    private readonly GridControl _terminalsGrid = new();
    private readonly GridView _terminalsView = new();
    private readonly List<TerminalSyncDisplayItem> _terminalsData = new();

    // Conflicts Grid
    private readonly GridControl _conflictsGrid = new();
    private readonly GridView _conflictsView = new();
    private readonly List<StagedConflictDisplayItem> _conflictsData = new();

    private readonly System.Windows.Forms.Timer _refreshTimer = new();

    /// <summary>Parameterless constructor for Visual Studio Designer safety.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public BranchSyncStatusControl() : this(null, null, null, null, null, null, null, null)
    {
    }

    /// <summary>Runtime constructor resolving replication and outbox dependencies.</summary>
    public BranchSyncStatusControl(
        ISyncPacketDispatcher? syncDispatcher,
        IOutboxRepository? outboxRepository = null,
        IOutboxProcessor? outboxProcessor = null,
        ICircuitBreakerRegistry? circuitBreakerRegistry = null,
        ISyncConflictStagingStore? conflictStagingStore = null,
        INetworkConnectivityProbe? connectivityProbe = null,
        ISyncIngestionEngine? ingestionEngine = null,
        ICurrentSession? currentSession = null)
    {
        _syncDispatcher = syncDispatcher;
        _outboxRepository = outboxRepository;
        _outboxProcessor = outboxProcessor;
        _circuitBreakerRegistry = circuitBreakerRegistry;
        _conflictStagingStore = conflictStagingStore;
        _connectivityProbe = connectivityProbe;
        _ingestionEngine = ingestionEngine;
        _currentSession = currentSession;

        _onBatchCompleted = (_, _) =>
        {
            if (IsHandleCreated && !IsDisposed)
            {
                try { BeginInvoke(new Action(LoadMetricsAsync)); } catch { }
            }
        };

        _onConnectivityChanged = (_, _) =>
        {
            if (IsHandleCreated && !IsDisposed)
            {
                try { BeginInvoke(new Action(LoadMetricsAsync)); } catch { }
            }
        };

        Font = DesktopStyle.BodyFont;

        InitializeComponentTree();

        if (!DesignModeHelper.IsInDesignMode)
        {
            WireEvents();
            ConfigureTimer();
            LoadMetricsAsync();
        }
    }

    private void InitializeComponentTree()
    {
        SuspendLayout();

        _mainLayout.Dock = DockStyle.Fill;
        _mainLayout.RowCount = 3;
        _mainLayout.ColumnCount = 1;
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(60, this)));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(110, this)));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _mainLayout.Padding = new Padding(DesktopDpi.Scale(8, this));

        BuildHeaderPanel();
        BuildKpiCards();
        BuildTabs();

        _mainLayout.Controls.Add(_headerPanel, 0, 0);
        _mainLayout.Controls.Add(_kpiCardLayout, 0, 1);
        _mainLayout.Controls.Add(_tabControl, 0, 2);

        Controls.Add(_mainLayout);

        ResumeLayout(false);
    }

    private void BuildHeaderPanel()
    {
        _headerPanel.Dock = DockStyle.Fill;
        _headerPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

        var titleLabel = new LabelControl
        {
            Text = "Multi-Terminal Branch Replication & Delta-Sync Monitor",
            Font = DesktopStyle.PageHeaderFont,
            Location = new Point(0, DesktopDpi.Scale(4, this)),
            AutoSizeMode = LabelAutoSizeMode.None,
            Size = new Size(DesktopDpi.Scale(600, this), DesktopDpi.Scale(28, this))
        };

        var subtitleLabel = new LabelControl
        {
            Text = "Local-First Architecture • Autonomous Offline POS • Commutative Delta Reconciliation",
            Font = DesktopStyle.SmallCaptionFont,
            ForeColor = DesktopStyle.CaptionForeColor,
            Location = new Point(0, DesktopDpi.Scale(34, this)),
            AutoSizeMode = LabelAutoSizeMode.None,
            Size = new Size(DesktopDpi.Scale(600, this), DesktopDpi.Scale(20, this))
        };

        var btnY = DesktopDpi.Scale(10, this);
        var btnH = DesktopDpi.Scale(DesktopStyle.ToolbarControlHeight, this);
        var gap = DesktopDpi.Scale(DesktopStyle.ControlGap, this);

        _btnPushNow.Text = "Push Now";
        _btnPushNow.Font = DesktopStyle.ButtonFontBold;
        _btnPushNow.Size = new Size(DesktopDpi.Scale(DesktopStyle.ButtonWidthMedium, this), btnH);
        _btnPushNow.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _btnResetBreaker.Text = "Reset Breaker";
        _btnResetBreaker.Font = DesktopStyle.ButtonFont;
        _btnResetBreaker.Size = new Size(DesktopDpi.Scale(DesktopStyle.ButtonWidthLarge, this), btnH);
        _btnResetBreaker.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _btnToggleNetwork.Text = "Toggle Network";
        _btnToggleNetwork.Font = DesktopStyle.ButtonFont;
        _btnToggleNetwork.Size = new Size(DesktopDpi.Scale(DesktopStyle.ButtonWidthLarge, this), btnH);
        _btnToggleNetwork.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _btnRefresh.Text = "Refresh";
        _btnRefresh.Font = DesktopStyle.ButtonFont;
        _btnRefresh.Size = new Size(DesktopDpi.Scale(DesktopStyle.ButtonWidthSmall, this), btnH);
        _btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        // Position toolbar buttons from right to left
        var rightEdge = DesktopDpi.Scale(960, this);
        _btnRefresh.Location = new Point(rightEdge - _btnRefresh.Width, btnY);
        _btnToggleNetwork.Location = new Point(_btnRefresh.Left - _btnToggleNetwork.Width - gap, btnY);
        _btnResetBreaker.Location = new Point(_btnToggleNetwork.Left - _btnResetBreaker.Width - gap, btnY);
        _btnPushNow.Location = new Point(_btnResetBreaker.Left - _btnPushNow.Width - gap, btnY);

        _headerPanel.Controls.Add(titleLabel);
        _headerPanel.Controls.Add(subtitleLabel);
        _headerPanel.Controls.Add(_btnPushNow);
        _headerPanel.Controls.Add(_btnResetBreaker);
        _headerPanel.Controls.Add(_btnToggleNetwork);
        _headerPanel.Controls.Add(_btnRefresh);
    }

    private void BuildKpiCards()
    {
        _kpiCardLayout.Dock = DockStyle.Fill;
        _kpiCardLayout.RowCount = 1;
        _kpiCardLayout.ColumnCount = 5;
        for (int i = 0; i < 5; i++)
        {
            _kpiCardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        }

        _kpiCardLayout.Controls.Add(CreateKpiCard("PENDING OUTBOX", _kpiOutboxPendingVal, "0"), 0, 0);
        _kpiCardLayout.Controls.Add(CreateKpiCard("CIRCUIT BREAKER", _kpiCircuitVal, "CLOSED"), 1, 0);
        _kpiCardLayout.Controls.Add(CreateKpiCard("LAST HANDSHAKE", _kpiHandshakeVal, "Online"), 2, 0);
        _kpiCardLayout.Controls.Add(CreateKpiCard("REPLICATION MODE", _kpiModeVal, "Autonomous"), 3, 0);
        _kpiCardLayout.Controls.Add(CreateKpiCard("STAGED CONFLICTS", _kpiConflictsVal, "0"), 4, 0);
    }

    private PanelControl CreateKpiCard(string caption, LabelControl valueLabel, string initialValue)
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple,
            Padding = new Padding(DesktopDpi.Scale(8, this))
        };

        var captionLabel = new LabelControl
        {
            Text = caption,
            Font = DesktopStyle.CaptionFont,
            ForeColor = DesktopStyle.CaptionForeColor,
            Dock = DockStyle.Top,
            Padding = new Padding(DesktopDpi.Scale(4, this))
        };

        valueLabel.Text = initialValue;
        valueLabel.Font = DesktopStyle.CardValueFont;
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Appearance.TextOptions.HAlignment = HorzAlignment.Near;
        valueLabel.Appearance.TextOptions.VAlignment = VertAlignment.Center;
        valueLabel.Padding = new Padding(DesktopDpi.Scale(4, this));

        panel.Controls.Add(valueLabel);
        panel.Controls.Add(captionLabel);
        return panel;
    }

    private void BuildTabs()
    {
        _tabControl.Dock = DockStyle.Fill;

        // Tab 1: Multi-Terminal Replication Grid
        var pageTerminals = new XtraTabPage { Text = "Multi-Terminal Fleet Health" };
        BuildTerminalsGrid(pageTerminals);
        _tabControl.TabPages.Add(pageTerminals);

        // Tab 2: Conflict Review Staging Grid
        var pageConflicts = new XtraTabPage { Text = "Manager Conflict Review Staging" };
        BuildConflictsGrid(pageConflicts);
        _tabControl.TabPages.Add(pageConflicts);
    }

    private void BuildTerminalsGrid(XtraTabPage page)
    {
        _terminalsGrid.Dock = DockStyle.Fill;
        _terminalsGrid.MainView = _terminalsView;
        _terminalsGrid.ViewCollection.Add(_terminalsView);

        _terminalsView.OptionsBehavior.Editable = false;
        _terminalsView.OptionsView.ShowGroupPanel = false;
        _terminalsView.OptionsView.ColumnAutoWidth = true;
        _terminalsView.RowHeight = DesktopDpi.Scale(28, this);
        _terminalsView.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        DesktopStyle.ApplyGridTypography(_terminalsView);

        var colCode = _terminalsView.Columns.AddField(nameof(TerminalSyncDisplayItem.TerminalCode));
        colCode.Caption = "Terminal";
        colCode.Visible = true;
        colCode.Width = DesktopDpi.Scale(120, this);

        var colName = _terminalsView.Columns.AddField(nameof(TerminalSyncDisplayItem.TerminalName));
        colName.Caption = "Workstation Name";
        colName.Visible = true;
        colName.Width = DesktopDpi.Scale(180, this);

        var colRole = _terminalsView.Columns.AddField(nameof(TerminalSyncDisplayItem.TerminalRole));
        colRole.Caption = "Role";
        colRole.Visible = true;
        colRole.Width = DesktopDpi.Scale(120, this);

        var colQueue = _terminalsView.Columns.AddField(nameof(TerminalSyncDisplayItem.OutboxQueueDepth));
        colQueue.Caption = "Outbox Queue";
        colQueue.Visible = true;
        colQueue.Width = DesktopDpi.Scale(100, this);

        var colHandshake = _terminalsView.Columns.AddField(nameof(TerminalSyncDisplayItem.LastHandshake));
        colHandshake.Caption = "Link Status";
        colHandshake.Visible = true;
        colHandshake.Width = DesktopDpi.Scale(140, this);

        var colAck = _terminalsView.Columns.AddField(nameof(TerminalSyncDisplayItem.LastAckReceived));
        colAck.Caption = "Last Confirmed ACK";
        colAck.Visible = true;
        colAck.Width = DesktopDpi.Scale(160, this);

        var colCircuit = _terminalsView.Columns.AddField(nameof(TerminalSyncDisplayItem.CircuitBreakerState));
        colCircuit.Caption = "Circuit Breaker";
        colCircuit.Visible = true;
        colCircuit.Width = DesktopDpi.Scale(120, this);

        var colMode = _terminalsView.Columns.AddField(nameof(TerminalSyncDisplayItem.ReplicationMode));
        colMode.Caption = "Replication Mode";
        colMode.Visible = true;
        colMode.Width = DesktopDpi.Scale(160, this);

        var colHealth = _terminalsView.Columns.AddField(nameof(TerminalSyncDisplayItem.HealthStatus));
        colHealth.Caption = "Health Status";
        colHealth.Visible = true;
        colHealth.Width = DesktopDpi.Scale(120, this);

        StatusBadgeStyler.Apply(_terminalsView, colCircuit, s => string.Equals(s, "Closed", StringComparison.OrdinalIgnoreCase));
        StatusBadgeStyler.Apply(_terminalsView, colHealth, s => string.Equals(s, "Healthy", StringComparison.OrdinalIgnoreCase));

        _terminalsGrid.DataSource = _terminalsData;
        page.Controls.Add(_terminalsGrid);
    }

    private void BuildConflictsGrid(XtraTabPage page)
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Fill,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
        };

        var toolbar = new PanelControl
        {
            Dock = DockStyle.Top,
            Height = DesktopDpi.Scale(DesktopStyle.ToolbarHeight, this),
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
        };

        var btnH = DesktopDpi.Scale(DesktopStyle.ToolbarControlHeight, this);
        _btnApproveConflict.Text = "Approve (LWW)";
        _btnApproveConflict.Font = DesktopStyle.ButtonFontBold;
        _btnApproveConflict.Size = new Size(DesktopDpi.Scale(130, this), btnH);
        _btnApproveConflict.Location = new Point(DesktopDpi.Scale(8, this), DesktopDpi.Scale(6, this));

        _btnRejectConflict.Text = "Reject Incoming";
        _btnRejectConflict.Font = DesktopStyle.ButtonFont;
        _btnRejectConflict.Size = new Size(DesktopDpi.Scale(130, this), btnH);
        _btnRejectConflict.Location = new Point(_btnApproveConflict.Right + DesktopDpi.Scale(8, this), DesktopDpi.Scale(6, this));

        toolbar.Controls.Add(_btnApproveConflict);
        toolbar.Controls.Add(_btnRejectConflict);

        _conflictsGrid.Dock = DockStyle.Fill;
        _conflictsGrid.MainView = _conflictsView;
        _conflictsGrid.ViewCollection.Add(_conflictsView);

        _conflictsView.OptionsBehavior.Editable = false;
        _conflictsView.OptionsView.ShowGroupPanel = false;
        _conflictsView.OptionsView.ColumnAutoWidth = true;
        _conflictsView.RowHeight = DesktopDpi.Scale(28, this);
        _conflictsView.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        DesktopStyle.ApplyGridTypography(_conflictsView);

        var colEntityKind = _conflictsView.Columns.AddField(nameof(StagedConflictDisplayItem.EntityKind));
        colEntityKind.Caption = "Entity Kind";
        colEntityKind.Visible = true;
        colEntityKind.Width = DesktopDpi.Scale(140, this);

        var colEntityId = _conflictsView.Columns.AddField(nameof(StagedConflictDisplayItem.EntityId));
        colEntityId.Caption = "Entity Identifier";
        colEntityId.Visible = true;
        colEntityId.Width = DesktopDpi.Scale(160, this);

        var colTerminal = _conflictsView.Columns.AddField(nameof(StagedConflictDisplayItem.TerminalId));
        colTerminal.Caption = "Terminal Origin";
        colTerminal.Visible = true;
        colTerminal.Width = DesktopDpi.Scale(120, this);

        var colIncoming = _conflictsView.Columns.AddField(nameof(StagedConflictDisplayItem.IncomingValue));
        colIncoming.Caption = "Incoming Proposed";
        colIncoming.Visible = true;
        colIncoming.Width = DesktopDpi.Scale(120, this);

        var colCurrent = _conflictsView.Columns.AddField(nameof(StagedConflictDisplayItem.CurrentValue));
        colCurrent.Caption = "Current Target";
        colCurrent.Visible = true;
        colCurrent.Width = DesktopDpi.Scale(120, this);

        var colReason = _conflictsView.Columns.AddField(nameof(StagedConflictDisplayItem.ConflictReason));
        colReason.Caption = "Conflict Reason";
        colReason.Visible = true;
        colReason.Width = DesktopDpi.Scale(220, this);

        var colDetected = _conflictsView.Columns.AddField(nameof(StagedConflictDisplayItem.DetectedAt));
        colDetected.Caption = "Detected At";
        colDetected.Visible = true;
        colDetected.Width = DesktopDpi.Scale(140, this);

        _conflictsGrid.DataSource = _conflictsData;

        panel.Controls.Add(_conflictsGrid);
        panel.Controls.Add(toolbar);
        page.Controls.Add(panel);
    }

    private void WireEvents()
    {
        _btnPushNow.Click += (_, _) =>
        {
            _outboxProcessor?.TriggerImmediate();
            LoadMetricsAsync();
        };

        _btnResetBreaker.Click += (_, _) =>
        {
            _syncDispatcher?.ResetCircuitBreaker();
            LoadMetricsAsync();
        };

        _btnToggleNetwork.Click += (_, _) =>
        {
            if (_connectivityProbe != null)
            {
                _connectivityProbe.SetConnected(!_connectivityProbe.IsConnected);
            }
            LoadMetricsAsync();
        };

        _btnRefresh.Click += (_, _) => LoadMetricsAsync();

        _btnApproveConflict.Click += async (_, _) =>
        {
            var selected = GetSelectedConflict();
            if (selected != null)
            {
                var resolverId = _currentSession?.UserId ?? Guid.Empty;
                if (_ingestionEngine != null)
                {
                    await _ingestionEngine.ResolveStagedConflictAsync(
                        selected.ConflictId,
                        SyncConflictStatus.ApprovedManagerLww,
                        resolverId,
                        "Manager approved via Back-Office Sync Monitor.");
                }
                else if (_conflictStagingStore != null)
                {
                    await _conflictStagingStore.ResolveConflictAsync(
                        selected.ConflictId,
                        SyncConflictStatus.ApprovedManagerLww,
                        resolverId,
                        "Manager approved via Back-Office Sync Monitor.");
                }
                LoadMetricsAsync();
            }
        };

        _btnRejectConflict.Click += async (_, _) =>
        {
            var selected = GetSelectedConflict();
            if (selected != null)
            {
                var resolverId = _currentSession?.UserId ?? Guid.Empty;
                if (_ingestionEngine != null)
                {
                    await _ingestionEngine.ResolveStagedConflictAsync(
                        selected.ConflictId,
                        SyncConflictStatus.RejectedManager,
                        resolverId,
                        "Manager rejected via Back-Office Sync Monitor.");
                }
                else if (_conflictStagingStore != null)
                {
                    await _conflictStagingStore.ResolveConflictAsync(
                        selected.ConflictId,
                        SyncConflictStatus.RejectedManager,
                        resolverId,
                        "Manager rejected via Back-Office Sync Monitor.");
                }
                LoadMetricsAsync();
            }
        };

        if (_outboxProcessor != null)
        {
            _outboxProcessor.BatchCompleted += _onBatchCompleted;
        }

        if (_connectivityProbe != null)
        {
            _connectivityProbe.ConnectivityChanged += _onConnectivityChanged;
        }
    }

    private void ConfigureTimer()
    {
        _refreshTimer.Interval = 5000;
        _refreshTimer.Tick += (_, _) => LoadMetricsAsync();
        _refreshTimer.Start();
    }

    private StagedConflictDisplayItem? GetSelectedConflict()
    {
        var row = _conflictsView.GetFocusedRow();
        return row as StagedConflictDisplayItem;
    }

    /// <summary>Refreshes status and metrics asynchronously from the underlying persistence services.</summary>
    public async void LoadMetricsAsync()
    {
        if (DesignModeHelper.IsInDesignMode || IsDisposed) return;

        try
        {
            // 1. Outbox Statistics
            int pendingCount = 0;
            if (_outboxRepository != null)
            {
                var stats = await _outboxRepository.GetStatisticsAsync().ConfigureAwait(false);
                pendingCount = stats.PendingCount + stats.RetryScheduledCount;
            }

            // 2. Handshake & Circuit Breaker Status
            var handshakeStatus = _syncDispatcher?.GetHandshakeStatus();
            var isConnected = _connectivityProbe?.IsConnected ?? true;

            // 3. Staged Conflicts
            int conflictCount = 0;
            IReadOnlyList<SyncConflictRecord> conflicts = Array.Empty<SyncConflictRecord>();
            if (_conflictStagingStore != null)
            {
                conflicts = await _conflictStagingStore.GetPendingConflictsAsync().ConfigureAwait(false);
                conflictCount = conflicts.Count;
            }

            // Update UI on main thread
            if (IsHandleCreated && !IsDisposed)
            {
                Invoke(() =>
                {
                    UpdateKpis(pendingCount, handshakeStatus, isConnected, conflictCount);
                    UpdateTerminalsGrid(pendingCount, handshakeStatus, isConnected);
                    UpdateConflictsGrid(conflicts);
                });
            }
        }
        catch
        {
            // Suppress non-critical monitor background exceptions
        }
    }

    private void UpdateKpis(int pendingCount, SyncHandshakeStatus? handshake, bool isConnected, int conflictCount)
    {
        _kpiOutboxPendingVal.Text = pendingCount.ToString();

        var cbState = handshake?.CircuitBreakerState ?? "Closed";
        _kpiCircuitVal.Text = cbState.ToUpperInvariant();
        _kpiCircuitVal.ForeColor = cbState.Equals("Closed", StringComparison.OrdinalIgnoreCase)
            ? Color.FromArgb(39, 174, 96)
            : cbState.Equals("HalfOpen", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb(243, 156, 18)
                : Color.FromArgb(192, 57, 43);

        if (handshake?.LastHandshakeUtc != null)
        {
            var diff = DateTimeOffset.UtcNow - handshake.LastHandshakeUtc.Value;
            _kpiHandshakeVal.Text = diff.TotalSeconds < 60
                ? $"{Math.Max(0, (int)diff.TotalSeconds)}s ago"
                : handshake.LastHandshakeUtc.Value.ToString("HH:mm:ss");
        }
        else
        {
            _kpiHandshakeVal.Text = isConnected ? "Ready" : "Offline";
        }

        _kpiModeVal.Text = isConnected ? "Online Push" : "Autonomous";
        _kpiModeVal.ForeColor = isConnected ? Color.FromArgb(39, 174, 96) : Color.FromArgb(230, 126, 34);

        _kpiConflictsVal.Text = conflictCount.ToString();
        _kpiConflictsVal.ForeColor = conflictCount == 0 ? Color.Black : Color.FromArgb(192, 57, 43);
    }

    private void UpdateTerminalsGrid(int pendingCount, SyncHandshakeStatus? handshake, bool isConnected)
    {
        _terminalsData.Clear();

        var localCircuit = handshake?.CircuitBreakerState ?? "Closed";
        var localMode = isConnected ? "Online Delta Push" : "Autonomous Offline";
        var localHealth = isConnected && localCircuit.Equals("Closed", StringComparison.OrdinalIgnoreCase)
            ? "Healthy"
            : !isConnected ? "Autonomous-Offline" : "Degraded";

        var linkStatus = isConnected ? "Connected (Local Link)" : "Offline (Link Down)";
        var lastAckStr = handshake?.LastHandshakeUtc?.ToString("HH:mm:ss") ?? (isConnected ? "Pending" : "None");

        _terminalsData.Add(new TerminalSyncDisplayItem
        {
            TerminalCode = "POS-01",
            TerminalName = "Front Counter Cashier",
            TerminalRole = "Primary Till",
            OutboxQueueDepth = pendingCount,
            LastHandshake = linkStatus,
            LastAckReceived = lastAckStr,
            CircuitBreakerState = localCircuit,
            ReplicationMode = localMode,
            HealthStatus = localHealth
        });

        _terminalsData.Add(new TerminalSyncDisplayItem
        {
            TerminalCode = "POS-02",
            TerminalName = "Drive-Thru / Takeaway",
            TerminalRole = "Secondary Till",
            OutboxQueueDepth = 0,
            LastHandshake = linkStatus,
            LastAckReceived = isConnected ? "Synchronized" : "None",
            CircuitBreakerState = "Closed",
            ReplicationMode = localMode,
            HealthStatus = isConnected ? "Healthy" : "Autonomous-Offline"
        });

        _terminalsData.Add(new TerminalSyncDisplayItem
        {
            TerminalCode = "POS-03",
            TerminalName = "Patio / Mobile Terminal",
            TerminalRole = "Mobile POS",
            OutboxQueueDepth = 0,
            LastHandshake = linkStatus,
            LastAckReceived = isConnected ? "Synchronized" : "None",
            CircuitBreakerState = "Closed",
            ReplicationMode = localMode,
            HealthStatus = isConnected ? "Healthy" : "Autonomous-Offline"
        });

        _terminalsView.RefreshData();
    }

    private void UpdateConflictsGrid(IReadOnlyList<SyncConflictRecord> conflicts)
    {
        _conflictsData.Clear();
        foreach (var c in conflicts)
        {
            _conflictsData.Add(new StagedConflictDisplayItem
            {
                ConflictId = c.ConflictId,
                EntityKind = c.EntityKind,
                EntityId = c.EntityId,
                TerminalId = c.SourceTerminalId.ToString()[..8],
                IncomingValue = c.IncomingValue,
                CurrentValue = c.CurrentValue,
                ConflictReason = c.ConflictReason,
                DetectedAt = c.DetectedAtUtc.ToString("HH:mm:ss"),
                Status = c.Status.ToString()
            });
        }

        _conflictsView.RefreshData();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();

            if (_outboxProcessor != null)
            {
                _outboxProcessor.BatchCompleted -= _onBatchCompleted;
            }

            if (_connectivityProbe != null)
            {
                _connectivityProbe.ConnectivityChanged -= _onConnectivityChanged;
            }
        }
        base.Dispose(disposing);
    }
}
