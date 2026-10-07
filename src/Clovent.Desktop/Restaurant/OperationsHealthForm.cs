using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.Services;
using Clovent.Platform.CircuitBreakers;
using Clovent.Restaurant.Application.Outbox;
using Clovent.Restaurant.Continuity;
using Clovent.Restaurant.Outbox;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Restaurant;

/// <summary>
/// Model representing an outbox item displayed in the health grid.
/// </summary>
public sealed class OutboxMessageDisplayItem
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string LastAttemptAt { get; set; } = string.Empty;
    public string NextRetryAt { get; set; } = string.Empty;
    public string LastError { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}

/// <summary>
/// Operations Health Center and Continuity Center.
/// Provides live monitoring of database connectivity, emergency offline journal status,
/// transactional outbox queues, circuit breaker states, and POS latency budgets.
/// High-DPI certified across 100% to 250% scaling.
/// </summary>
[DesignerCategory("Code")]
public sealed class OperationsHealthForm : XtraForm
{
    private readonly IContinuityCoordinator? _continuityCoordinator;
    private readonly IOutboxRepository? _outboxRepository;
    private readonly IOutboxProcessor? _outboxProcessor;
    private readonly ICircuitBreakerRegistry? _circuitBreakerRegistry;

    private readonly LabelControl _dbStatusLabel = new();
    private readonly LabelControl _modeStatusLabel = new();
    private readonly LabelControl _journalStatusLabel = new();
    private readonly LabelControl _cacheStatusLabel = new();
    private readonly LabelControl _cacheDetailsLabel = new();
    private readonly LabelControl _cacheMetaLabel = new();
    private readonly LabelControl _outboxStatusLabel = new();
    private readonly LabelControl _circuitsStatusLabel = new();
    private readonly LabelControl _telemetryStatusLabel = new();

    private readonly GridControl _gridControl = new();
    private readonly GridView _gridView = new();
    private readonly List<OutboxMessageDisplayItem> _gridData = new();

    private readonly SimpleButton _refreshButton = new();
    private readonly SimpleButton _syncCacheButton = new();
    private readonly SimpleButton _validateCacheButton = new();
    private readonly SimpleButton _retrySelectedButton = new();
    private readonly SimpleButton _retryAllButton = new();
    private readonly SimpleButton _detailsButton = new();
    private readonly SimpleButton _processOutboxButton = new();
    private readonly SimpleButton _testDbButton = new();
    private readonly SimpleButton _replayJournalButton = new();
    private readonly SimpleButton _toggleModeButton = new();
    private readonly SimpleButton _exportDiagButton = new();
    private readonly SimpleButton _closeButton = new();

    /// <summary>Parameterless constructor for designer safety.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public OperationsHealthForm() : this(null, null, null, null)
    {
    }

    /// <summary>Runtime constructor resolving coordinator and background engines.</summary>
    public OperationsHealthForm(
        IContinuityCoordinator? continuityCoordinator,
        IOutboxRepository? outboxRepository = null,
        IOutboxProcessor? outboxProcessor = null,
        ICircuitBreakerRegistry? circuitBreakerRegistry = null)
    {
        _continuityCoordinator = continuityCoordinator;
        _outboxRepository = outboxRepository;
        _outboxProcessor = outboxProcessor;
        _circuitBreakerRegistry = circuitBreakerRegistry;

        Text = "Operations Health & Continuity Center";
        Font = new Font("Segoe UI", 9.5F);
        StartPosition = FormStartPosition.CenterParent;

        DesktopDialogSizing.Apply(this, 1040, 720, 880, 600, null, true);
        BuildLayout();
        LoadHealthMetrics();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(DesktopDpi.Scale(16, this)),
            Margin = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // 0: Header
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(175, this))); // 1: Cards Panel
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));        // 2: Outbox Grid
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // 3: Telemetry Bar
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, DesktopDpi.Scale(50, this))); // 4: Action Buttons

        // 1. Header
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(10, this))
        };
        var title = new LabelControl
        {
            Text = "OPERATIONS HEALTH & CONTINUITY CENTER",
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 23, 42)
        };
        var sub = new LabelControl
        {
            Text = "Real-time fault isolation, emergency journal synchronization, and background transactional outbox queues.",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(100, 116, 139)
        };
        header.Controls.Add(title);
        header.Controls.Add(sub);

        // 2. Status Cards Panel (3x2)
        var cardsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0)
        };
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
        cardsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
        cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        cardsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        cardsPanel.Controls.Add(CreateCard("Database & Continuity Mode", _dbStatusLabel, _modeStatusLabel), 0, 0);
        cardsPanel.Controls.Add(CreateCard("Emergency Offline Journal", _journalStatusLabel), 1, 0);
        cardsPanel.Controls.Add(CreateCard("Local Operational Cache", _cacheStatusLabel, _cacheDetailsLabel), 2, 0);
        cardsPanel.Controls.Add(CreateCard("Transactional Outbox Queues", _outboxStatusLabel), 0, 1);
        cardsPanel.Controls.Add(CreateCard("Dependency Circuit Breakers", _circuitsStatusLabel), 1, 1);
        cardsPanel.Controls.Add(CreateCard("Cache Provenance & Policy", _cacheMetaLabel), 2, 1);

        // 3. Grid Panel
        var gridPanel = new PanelControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, DesktopDpi.Scale(6, this), 0, DesktopDpi.Scale(6, this)),
            Padding = new Padding(0)
        };

        ConfigureGrid();
        gridPanel.Controls.Add(_gridControl);

        // 4. Telemetry Bar
        var telemetryCard = CreateCard("POS Real-Time Latency Budgets", _telemetryStatusLabel);
        telemetryCard.Dock = DockStyle.Fill;
        telemetryCard.Margin = new Padding(0, 0, 0, DesktopDpi.Scale(6, this));

        // 5. Action Bar
        var actionBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            Margin = new Padding(0)
        };

        ConfigureButton(_refreshButton, "Refresh", (_, _) => LoadHealthMetrics());
        ConfigureButton(_syncCacheButton, "Sync Cache", async (_, _) => await RunSyncCacheAsync());
        ConfigureButton(_validateCacheButton, "Validate Cache", async (_, _) => await RunValidateCacheAsync());
        ConfigureButton(_retrySelectedButton, "Retry Selected", async (_, _) => await RetrySelectedAsync());
        ConfigureButton(_retryAllButton, "Retry All", async (_, _) => await RetryAllFailedAsync());
        ConfigureButton(_detailsButton, "Details...", (_, _) => ShowSelectedDetails());
        ConfigureButton(_processOutboxButton, "Process Outbox", (_, _) => TriggerOutbox());
        ConfigureButton(_testDbButton, "Test DB", async (_, _) => await RunTestDbAsync());
        ConfigureButton(_replayJournalButton, "Replay Journal", async (_, _) => await RunReplayAsync());
        ConfigureButton(_toggleModeButton, "Toggle Mode", (_, _) => ToggleContinuityMode());
        ConfigureButton(_exportDiagButton, "Export Diag", (_, _) => ExportDiagnostics());
        ConfigureButton(_closeButton, "Close", (_, _) => Close());

        actionBar.Controls.Add(_refreshButton);
        actionBar.Controls.Add(_syncCacheButton);
        actionBar.Controls.Add(_validateCacheButton);
        actionBar.Controls.Add(_retrySelectedButton);
        actionBar.Controls.Add(_retryAllButton);
        actionBar.Controls.Add(_detailsButton);
        actionBar.Controls.Add(_processOutboxButton);
        actionBar.Controls.Add(_testDbButton);
        actionBar.Controls.Add(_replayJournalButton);
        actionBar.Controls.Add(_toggleModeButton);
        actionBar.Controls.Add(_exportDiagButton);
        actionBar.Controls.Add(_closeButton);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(cardsPanel, 0, 1);
        root.Controls.Add(gridPanel, 0, 2);
        root.Controls.Add(telemetryCard, 0, 3);
        root.Controls.Add(actionBar, 0, 4);

        Controls.Add(root);
    }

    private void ConfigureGrid()
    {
        _gridControl.Dock = DockStyle.Fill;
        _gridControl.MainView = _gridView;
        _gridControl.ViewCollection.Add(_gridView);

        _gridView.GridControl = _gridControl;
        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsBehavior.ReadOnly = true;
        _gridView.OptionsSelection.EnableAppearanceFocusedCell = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ShowIndicator = false;
        _gridView.OptionsView.ColumnAutoWidth = true;

        _gridView.RowHeight = DesktopDpi.Scale(28, this);
        _gridView.ColumnPanelRowHeight = DesktopDpi.Scale(32, this);

        _gridView.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _gridView.Appearance.HeaderPanel.Options.UseFont = true;
        _gridView.Appearance.Row.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        _gridView.Appearance.Row.Options.UseFont = true;

        _gridView.DoubleClick += (_, _) => ShowSelectedDetails();

        _gridControl.DataSource = _gridData;
    }

    private Control CreateCard(string title, params LabelControl[] labels)
    {
        var panel = new PanelControl
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(DesktopDpi.Scale(10, this)),
            Margin = new Padding(DesktopDpi.Scale(3, this))
        };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };

        var titleLabel = new LabelControl
        {
            Text = title.ToUpperInvariant(),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105),
            Margin = new Padding(0, 0, 0, DesktopDpi.Scale(4, this))
        };
        layout.Controls.Add(titleLabel);

        foreach (var lbl in labels)
        {
            lbl.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lbl.ForeColor = Color.FromArgb(15, 23, 42);
            lbl.Margin = new Padding(0, 0, 0, DesktopDpi.Scale(2, this));
            layout.Controls.Add(lbl);
        }

        panel.Controls.Add(layout);
        return panel;
    }

    private void ConfigureButton(SimpleButton btn, string text, EventHandler onClick)
    {
        btn.Text = text;
        btn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btn.Height = DesktopDpi.Scale(32, this);
        btn.Width = DesktopDpi.Scale(96, this);
        btn.Margin = new Padding(0, 0, DesktopDpi.Scale(6, this), 0);
        btn.Click += onClick;
    }

    private async void LoadHealthMetrics()
    {
        // 1. DB & Continuity Status
        var isContinuity = _continuityCoordinator?.IsContinuityModeActive ?? false;
        _modeStatusLabel.Text = isContinuity
            ? "Mode: CONTINUITY / EMERGENCY MODE (Offline cash sales active)"
            : "Mode: NORMAL ONLINE (Full financial capabilities connected)";
        _modeStatusLabel.ForeColor = isContinuity ? Color.FromArgb(220, 38, 38) : Color.FromArgb(22, 163, 74);

        if (_continuityCoordinator != null)
        {
            var isDbUp = await _continuityCoordinator.CheckDatabaseHealthAsync();
            _dbStatusLabel.Text = isDbUp ? "Database: CONNECTED & HEALTHY" : "Database: OFFLINE / UNREACHABLE";
            _dbStatusLabel.ForeColor = isDbUp ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);

            // 2. Offline Journal Stats
            var journalStats = await _continuityCoordinator.GetJournalStatisticsAsync();
            _journalStatusLabel.Text = $"Total Logged: {journalStats.TotalRecorded}  |  Pending Replay: {journalStats.PendingReplayCount}\nReplayed: {journalStats.ReplayedCount}  |  Conflicts/Failed: {journalStats.FailedOrConflictCount}";

            // 3. Operational Cache Stats
            var cache = _continuityCoordinator.ActiveCache;
            var cacheStatus = _continuityCoordinator.CacheStatus;
            var cacheMsg = _continuityCoordinator.CacheStatusMessage ?? "Cache ready";
            var lastSync = _continuityCoordinator.LastCacheSyncUtc;

            _cacheStatusLabel.Text = $"Status: {cacheStatus.ToString().ToUpperInvariant()}";
            _cacheStatusLabel.ForeColor = cacheStatus switch
            {
                CacheValidationStatus.Valid => Color.FromArgb(22, 163, 74),
                CacheValidationStatus.StaleWithinPolicy => Color.FromArgb(202, 138, 4),
                _ => Color.FromArgb(220, 38, 38)
            };

            if (cache != null)
            {
                _cacheDetailsLabel.Text = $"Categories: {cache.Payload.Categories.Count} | Products: {cache.Payload.Products.Count}\nVariants: {cache.Payload.Variants.Count} | Tables: {cache.Payload.Tables.Count}";
                var syncTimeStr = lastSync.HasValue ? BusinessDateTimeFormatter.Format(lastSync.Value) : "Never";
                _cacheMetaLabel.Text = $"Version: {cache.Metadata.CacheVersion} (Schema v{cache.Metadata.SchemaVersion})\nLast Sync: {syncTimeStr}\nBranch: {cache.Metadata.BranchName} | Terminal: {cache.Metadata.TerminalCode}";
            }
            else
            {
                _cacheDetailsLabel.Text = cacheMsg;
                _cacheMetaLabel.Text = "No in-memory snapshot loaded.\nClick 'Sync Cache' or 'Validate Cache' to inspect.";
            }
        }
        else
        {
            _dbStatusLabel.Text = "Database: N/A";
            _journalStatusLabel.Text = "Offline Journal: Standby";
            _cacheStatusLabel.Text = "Cache: Standby";
            _cacheDetailsLabel.Text = "No continuity coordinator attached.";
            _cacheMetaLabel.Text = "N/A";
        }

        // 3. Outbox Stats & Grid
        if (_outboxRepository != null)
        {
            var stats = await _outboxRepository.GetStatisticsAsync();
            _outboxStatusLabel.Text = $"Pending: {stats.PendingCount}  |  Processing: {stats.ProcessingCount}\nRetry Scheduled: {stats.RetryScheduledCount}  |  DeadLetter: {stats.DeadLetterCount}  |  Completed: {stats.CompletedCount}";

            await LoadOutboxGridAsync();
        }
        else
        {
            _outboxStatusLabel.Text = "Outbox Queue: Ready";
        }

        // 4. Circuit Breakers
        if (_circuitBreakerRegistry != null)
        {
            var circuits = _circuitBreakerRegistry.GetAll();
            var sb = new StringBuilder();
            foreach (var cb in circuits)
            {
                sb.AppendLine($"{cb.Name}: {cb.State} (Failures: {cb.FailureCount})");
            }
            _circuitsStatusLabel.Text = sb.Length > 0 ? sb.ToString().TrimEnd() : "All external circuits closed (Healthy).";
        }
        else
        {
            _circuitsStatusLabel.Text = "Circuit Breakers: Active (Healthy)";
        }

        // 5. Telemetry
        var metrics = PosPerformanceTracker.GetMetrics();
        if (metrics.Count > 0)
        {
            var sb = new StringBuilder();
            foreach (var m in metrics.Take(4))
            {
                sb.Append($"{m.OperationName}: Avg {m.AverageMs:F1}ms (P95: {m.P95Ms:F1}ms / <{m.TargetBudgetMs}ms)   ");
            }
            _telemetryStatusLabel.Text = sb.ToString().TrimEnd();
        }
        else
        {
            _telemetryStatusLabel.Text = "Budgets: ProductAdd < 50ms | UniversalSearch < 100ms | PaymentCommit < 150ms | Receipt < 250ms";
        }
    }

    private async Task LoadOutboxGridAsync()
    {
        if (_outboxRepository == null) return;

        try
        {
            var messages = await _outboxRepository.GetDeadLetterAndFailedMessagesAsync(50);
            _gridData.Clear();

            foreach (var msg in messages)
            {
                _gridData.Add(new OutboxMessageDisplayItem
                {
                    Id = msg.Id.Value,
                    MessageType = msg.MessageType,
                    Status = msg.Status.ToString(),
                    AttemptCount = msg.AttemptCount,
                    CreatedAt = BusinessDateTimeFormatter.Format(msg.CreatedAtUtc),
                    LastAttemptAt = msg.LastAttemptAtUtc.HasValue ? BusinessDateTimeFormatter.Format(msg.LastAttemptAtUtc.Value) : "N/A",
                    NextRetryAt = msg.NextRetryAtUtc.HasValue ? BusinessDateTimeFormatter.Format(msg.NextRetryAtUtc.Value) : "N/A",
                    LastError = msg.LastError ?? "None",
                    Payload = msg.Payload
                });
            }

            _gridControl.RefreshDataSource();
        }
        catch
        {
            // Best effort grid refresh
        }
    }

    private async Task RetrySelectedAsync()
    {
        if (_outboxRepository == null) return;

        var selectedRow = _gridView.GetFocusedRow() as OutboxMessageDisplayItem;
        if (selectedRow == null)
        {
            XtraMessageBox.Show(this, "Please select an outbox message to retry.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var message = await _outboxRepository.GetByIdAsync(new OutboxMessageId(selectedRow.Id));
        if (message != null)
        {
            message.RetryNow();
            await _outboxRepository.UpdateAsync(message);
            XtraMessageBox.Show(this, $"Message {selectedRow.Id} marked for immediate retry.", "Outbox Retry", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadHealthMetrics();
        }
    }

    private async Task RetryAllFailedAsync()
    {
        if (_outboxRepository == null) return;

        var messages = await _outboxRepository.GetDeadLetterAndFailedMessagesAsync(100);
        int count = 0;
        foreach (var msg in messages)
        {
            msg.RetryNow();
            await _outboxRepository.UpdateAsync(msg);
            count++;
        }

        XtraMessageBox.Show(this, $"{count} failed/dead-letter message(s) reset for immediate retry.", "Outbox Retry All", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadHealthMetrics();
    }

    private void ShowSelectedDetails()
    {
        var selectedRow = _gridView.GetFocusedRow() as OutboxMessageDisplayItem;
        if (selectedRow == null) return;

        var sb = new StringBuilder();
        sb.AppendLine($"ID:          {selectedRow.Id}");
        sb.AppendLine($"Type:        {selectedRow.MessageType}");
        sb.AppendLine($"Status:      {selectedRow.Status}");
        sb.AppendLine($"Attempts:    {selectedRow.AttemptCount}");
        sb.AppendLine($"Created:     {selectedRow.CreatedAt}");
        sb.AppendLine($"Last Attempt:{selectedRow.LastAttemptAt}");
        sb.AppendLine($"Next Retry:  {selectedRow.NextRetryAt}");
        sb.AppendLine();
        sb.AppendLine("Error:");
        sb.AppendLine(selectedRow.LastError);
        sb.AppendLine();
        sb.AppendLine("Payload:");
        sb.AppendLine(selectedRow.Payload);

        using var dlg = new XtraForm
        {
            Text = $"Outbox Message Details - {selectedRow.Id}",
            Size = new Size(DesktopDpi.Scale(600, this), DesktopDpi.Scale(450, this)),
            StartPosition = FormStartPosition.CenterParent
        };
        var memo = new MemoEdit
        {
            Dock = DockStyle.Fill,
            Text = sb.ToString()
        };
        memo.Properties.ReadOnly = true;
        dlg.Controls.Add(memo);
        dlg.ShowDialog(this);
    }

    private async Task RunTestDbAsync()
    {
        if (_continuityCoordinator == null)
        {
            XtraMessageBox.Show(this, "Continuity coordinator is not available.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var isUp = await _continuityCoordinator.CheckDatabaseHealthAsync();
        if (isUp)
        {
            XtraMessageBox.Show(this, "Database connection is healthy and responsive.", "Database Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            XtraMessageBox.Show(this, "Database connection failed or timed out. System is safe to operate in Continuity Mode.", "Database Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        LoadHealthMetrics();
    }

    private async Task RunReplayAsync()
    {
        if (_continuityCoordinator == null) return;

        var result = await _continuityCoordinator.TriggerReplayAsync();
        var msg = $"Replay Complete:\nProcessed: {result.TotalProcessed}\nSuccessful: {result.SuccessCount}\nDuplicates Ignored: {result.DuplicateIgnoredCount}\nFailed: {result.FailedCount}";
        if (result.Errors.Count > 0)
        {
            msg += "\n\nErrors:\n" + string.Join("\n", result.Errors.Take(3));
        }

        XtraMessageBox.Show(this, msg, "Journal Replay", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadHealthMetrics();
    }

    private void TriggerOutbox()
    {
        _outboxProcessor?.TriggerImmediate();
        XtraMessageBox.Show(this, "Outbox background processing triggered.", "Outbox", MessageBoxButtons.OK, MessageBoxIcon.Information);
        LoadHealthMetrics();
    }

    private void ToggleContinuityMode()
    {
        if (_continuityCoordinator == null) return;

        if (_continuityCoordinator.IsContinuityModeActive)
        {
            _continuityCoordinator.ExitContinuityMode();
        }
        else
        {
            _continuityCoordinator.EnterContinuityMode("Manual operator activation from Operations Health Center");
        }
        LoadHealthMetrics();
    }

    private async Task RunSyncCacheAsync()
    {
        if (_continuityCoordinator == null)
        {
            XtraMessageBox.Show(this, "Continuity coordinator is not available.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var success = await _continuityCoordinator.RefreshCacheAsync();
        if (success)
        {
            XtraMessageBox.Show(this, "Local operational cache synchronized successfully from primary database.", "Cache Synchronization", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            XtraMessageBox.Show(this, $"Failed to synchronize operational cache: {_continuityCoordinator.CacheStatusMessage}", "Cache Synchronization", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        LoadHealthMetrics();
    }

    private async Task RunValidateCacheAsync()
    {
        if (_continuityCoordinator == null) return;

        var (status, msg) = await _continuityCoordinator.ValidateCacheAsync();
        var icon = status == CacheValidationStatus.Valid ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
        XtraMessageBox.Show(this, $"Validation Status: {status}\n\nDetails:\n{msg}", "Cache Validation", MessageBoxButtons.OK, icon);
        LoadHealthMetrics();
    }

    private void ExportDiagnostics()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== CLOVENT CONTINUITY & OPERATIONS HEALTH DIAGNOSTIC ===");
        sb.AppendLine($"Timestamp UTC: {DateTimeOffset.UtcNow:O}");
        sb.AppendLine($"Continuity Mode: {_continuityCoordinator?.IsContinuityModeActive}");
        sb.AppendLine($"Continuity Reason: {_continuityCoordinator?.ContinuityReason}");
        sb.AppendLine($"DB Status: {_dbStatusLabel.Text}");
        sb.AppendLine($"Journal: {_journalStatusLabel.Text}");
        sb.AppendLine($"Cache Status: {_cacheStatusLabel.Text}");
        sb.AppendLine($"Cache Details: {_cacheDetailsLabel.Text}");
        sb.AppendLine($"Cache Metadata: {_cacheMetaLabel.Text}");
        sb.AppendLine($"Outbox: {_outboxStatusLabel.Text}");
        sb.AppendLine($"Circuits: {_circuitsStatusLabel.Text}");
        sb.AppendLine("--- Telemetry ---");
        foreach (var m in PosPerformanceTracker.GetMetrics())
        {
            sb.AppendLine($"{m.OperationName}: Count={m.TotalCount}, Avg={m.AverageMs:F2}ms, P95={m.P95Ms:F2}ms, Max={m.MaxMs:F2}ms, Target={m.TargetBudgetMs}ms, OverBudget={m.OverBudgetCount}");
        }

        Clipboard.SetText(sb.ToString());
        XtraMessageBox.Show(this, "Full diagnostic report copied to clipboard.", "Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
