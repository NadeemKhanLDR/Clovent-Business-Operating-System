using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Forms.Base.Localization;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.PaymentMethods.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Forms.Restaurant.Setup;

/// <summary>
/// Restaurant Setup View: Unified settings screen for restaurant order numbering,
/// display language, and POS layout preferences.
/// Uses a single cohesive Save Settings action, unified enterprise cards,
/// and DPI-aware scaling.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class RestaurantSetupView : XtraUserControl
{
    private const string FeatureCode = "restaurantsetup";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private static readonly (string CultureCode, string Display)[] LanguageOptions = [("en", "English"), ("ur", "اردو (Urdu)")];

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RestaurantSetupView()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;

        InitializeComponent();
        InitializeLanguageOptions();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public RestaurantSetupView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession) : base()
    {
        InitializeComponent();
        InitializeLanguageOptions();

        if (DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _featurePolicy = null!;
            _currentSession = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;

        InitializeRuntime();
    }

    private void InitializeRuntime()
    {
        AppearanceManager.Changed += AppearanceManager_Changed;
        Resize += (_, _) => ScaleLayoutAtRuntime();
        ScaleLayoutAtRuntime();
    }

    private void InitializeLanguageOptions()
    {
        _languageCombo.Properties.Items.Clear();
        foreach (var option in LanguageOptions)
        {
            _languageCombo.Properties.Items.Add(option.Display);
        }
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) => AppearanceManager.Apply(this, "Restaurant", nameof(RestaurantSetupView));

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _scope?.Dispose();
            _gate?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void PrefixEdit_EditValueChanged(object? sender, EventArgs e) => UpdatePreview();

    private void StartingNumberEdit_EditValueChanged(object? sender, EventArgs e) => UpdatePreview();

    private async void SaveSettingsButton_Click(object? sender, EventArgs e) => await SaveSettingsAsync();

    /// <summary>Backward compatibility wrapper for callers invoking SaveAsync directly.</summary>
    public Task SaveAsync() => SaveSettingsAsync();

    private async void RestaurantSetupView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        ScaleLayoutAtRuntime();
        await LoadAsync();
    }

    private void ScaleLayoutAtRuntime()
    {
        if (DesignModeHelper.IsInDesignMode) return;

        _headerPanel.Height = DesktopDpi.Scale(54, this);
        _headerPanel.Padding = new Padding(DesktopDpi.Scale(24, this), DesktopDpi.Scale(10, this), DesktopDpi.Scale(24, this), DesktopDpi.Scale(4, this));
        _scrollContainer.Padding = new Padding(DesktopDpi.Scale(24, this), DesktopDpi.Scale(4, this), DesktopDpi.Scale(24, this), DesktopDpi.Scale(12, this));

        // Constrain settings content max width to avoid overly stretched cards on ultra-wide screens
        int maxContentW = DesktopDpi.Scale(840, this);
        _contentLayout.MaximumSize = new Size(maxContentW, 0);

        _prefixEdit.Width = DesktopDpi.Scale(200, this);
        _startingNumberEdit.Width = DesktopDpi.Scale(200, this);
        _languageCombo.Width = DesktopDpi.Scale(240, this);
        int editorH = DesktopDpi.Scale(26, this);
        _itemsPerRowCombo.Width = DesktopDpi.Scale(120, this);
        _activeOrdersRadioGroup.Width = DesktopDpi.Scale(220, this);
        _activeOrdersRadioGroup.Height = editorH;
        _activeOrdersRadioGroup.MaximumSize = new Size(0, editorH);
        _activeOrdersRadioGroup.Margin = new Padding(0, DesktopDpi.Scale(2, this), 0, DesktopDpi.Scale(2, this));
        _defaultPaymentMethodCombo.Width = DesktopDpi.Scale(240, this);

        _saveSettingsButton.Size = new Size(DesktopDpi.Scale(150, this), DesktopDpi.Scale(38, this));

        // Update card label column widths to be identical
        int labelColWidth = DesktopDpi.Scale(200, this);
        UpdateCardColumnWidth(_numberingCard, labelColWidth);
        UpdateCardColumnWidth(_languageCard, labelColWidth);
        UpdateCardColumnWidth(_posCard, labelColWidth);

        EnsureCardHeight(_numberingCard);
        EnsureCardHeight(_languageCard);
        EnsureCardHeight(_posCard);
    }

    private void EnsureCardHeight(GroupControl card)
    {
        card.AutoSize = true;
        foreach (Control ctrl in card.Controls)
        {
            if (ctrl is TableLayoutPanel table)
            {
                table.AutoSize = true;
                table.PerformLayout();
                var pref = table.GetPreferredSize(new Size(card.Width > 0 ? card.Width : DesktopDpi.Scale(600, this), 0));
                int captionH = DesktopDpi.Scale(32, this);
                int paddingH = DesktopDpi.Scale(16, this);
                int minH = pref.Height + captionH + paddingH;
                card.MinimumSize = new Size(0, minH);
            }
        }
    }

    private static void UpdateCardColumnWidth(GroupControl card, int labelColWidth)
    {
        foreach (Control ctrl in card.Controls)
        {
            if (ctrl is TableLayoutPanel table && table.ColumnStyles.Count >= 2)
            {
                table.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, labelColWidth);
            }
        }
    }

    private void UpdatePreview() => _previewLabel.Text = $"{_prefixEdit.Text.Trim()}{(int)_startingNumberEdit.Value}";

    private async Task LoadAsync()
    {
        AppearanceManager.Apply(this, "Restaurant", nameof(RestaurantSetupView));

        // 1. Order Number Sequence
        var sequence = await _mediator.Send(new GetOrderNumberSequenceQuery());
        _prefixEdit.Text = sequence.Prefix;
        _startingNumberEdit.Value = sequence.NextNumber;
        UpdatePreview();

        // 2. Language Preference
        var currentCultureCode = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        var selectedIndex = Array.FindIndex(LanguageOptions, o => o.CultureCode == currentCultureCode);
        _languageCombo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;

        // 3. POS Settings
        var itemsPerRow = PosSettingsStore.LoadItemsPerRow();
        _itemsPerRowCombo.SelectedItem = itemsPerRow;
        if (_itemsPerRowCombo.SelectedIndex < 0)
        {
            _itemsPerRowCombo.SelectedItem = 4;
        }

        _activeOrdersRadioGroup.EditValue = PosSettingsStore.LoadActiveOrdersCollapsed();

        try
        {
            var paymentMethods = await _mediator.Send(new ListPaymentMethodsQuery());
            _defaultPaymentMethodCombo.Properties.Items.Clear();
            foreach (var pm in paymentMethods)
            {
                _defaultPaymentMethodCombo.Properties.Items.Add(pm.Name);
            }
        }
        catch
        {
            _defaultPaymentMethodCombo.Properties.Items.Clear();
            _defaultPaymentMethodCombo.Properties.Items.AddRange(new[] { "Cash", "Card", "Mobile Wallet", "Bank Transfer" });
        }

        var defaultMethod = PosSettingsStore.LoadDefaultPaymentMethod();
        _defaultPaymentMethodCombo.SelectedItem = defaultMethod;
        if (_defaultPaymentMethodCombo.SelectedIndex < 0 && _defaultPaymentMethodCombo.Properties.Items.Count > 0)
        {
            _defaultPaymentMethodCombo.SelectedIndex = 0;
        }

        _statusLabel.Text = string.Empty;
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal Action<IWin32Window, string, string, MessageBoxButtons, MessageBoxIcon>? CustomMessageBoxShow { get; set; }

    private void ShowMessage(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        if (CustomMessageBoxShow is not null)
        {
            CustomMessageBoxShow(owner, text, caption, buttons, icon);
            return;
        }

        XtraMessageBox.Show(owner, text, caption, buttons, icon);
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    /// <summary>
    /// Unified save handler: Validates all inputs and persists Order Numbering,
    /// Display Language, and POS Layout settings in one coordinated action.
    /// </summary>
    public async Task SaveSettingsAsync()
    {
        if (!await CanUseFeatureAsync("edit"))
        {
            ShowMessage(this, "You do not have permission to edit the Restaurant Setup.", "Not Authorized", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // --- VALIDATION PHASE ---
        var prefix = _prefixEdit.Text.Trim();
        if (string.IsNullOrEmpty(prefix))
        {
            ShowMessage(this, "Enter an order number prefix.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _prefixEdit.Focus();
            return;
        }

        if (prefix.Length > 20)
        {
            ShowMessage(this, "Order number prefix cannot exceed 20 characters.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _prefixEdit.Focus();
            return;
        }

        var startingNumber = (int)_startingNumberEdit.Value;
        if (startingNumber < 1)
        {
            ShowMessage(this, "Starting number must be at least 1.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _startingNumberEdit.Focus();
            return;
        }

        _statusLabel.Text = string.Empty;

        // --- PERSISTENCE PHASE ---
        try
        {
            // 1. Order Number Sequence (Domain Command)
            var sequence = await _mediator.Send(new ConfigureOrderNumberSequenceCommand(prefix, startingNumber));
            _prefixEdit.Text = sequence.Prefix;
            _startingNumberEdit.Value = sequence.NextNumber;
            UpdatePreview();

            // 2. Language Preference
            string cultureCode = "en";
            if (_languageCombo.SelectedIndex >= 0 && _languageCombo.SelectedIndex < LanguageOptions.Length)
            {
                cultureCode = LanguageOptions[_languageCombo.SelectedIndex].CultureCode;
                LanguagePreferenceStore.Save(cultureCode);

                var culture = CultureInfo.GetCultureInfo(cultureCode);
                Thread.CurrentThread.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }

            // 3. POS Layout Settings
            int itemsPerRow = _itemsPerRowCombo.SelectedItem is int val ? val : 4;
            PosSettingsStore.SaveItemsPerRow(itemsPerRow);

            bool hideActiveOrders = _activeOrdersRadioGroup.EditValue is bool b && b;
            PosSettingsStore.SaveActiveOrdersCollapsed(hideActiveOrders);

            string defaultMethod = _defaultPaymentMethodCombo.SelectedItem is string dm && !string.IsNullOrWhiteSpace(dm)
                ? dm
                : "Cash";
            PosSettingsStore.SaveDefaultPaymentMethod(defaultMethod);

            // 4. Activity Log (Audit)
            try
            {
                await _mediator.Send(new RecordActivityCommand(
                    "Setup Changes",
                    $"Restaurant Setup saved: Order prefix '{sequence.Prefix}', next number {sequence.NextNumber}, language '{cultureCode}', items per row {itemsPerRow}, active orders {(hideActiveOrders ? "hidden" : "visible")}, default payment '{defaultMethod}'.",
                    _currentSession.DisplayName ?? "Unknown",
                    Environment.MachineName));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // Swallowed per auditing guidelines
            }

            _statusLabel.Text = "Settings saved successfully.";
            _statusLabel.ForeColor = Color.FromArgb(13, 148, 136); // Teal
        }
        catch (Clovent.Restaurant.RestaurantDomainException ex)
        {
            ShowMessage(this, ex.Message, "Invalid Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _statusLabel.Text = ex.Message;
            _statusLabel.ForeColor = Color.FromArgb(220, 38, 38);
        }
        catch (Exception ex)
        {
            ShowMessage(this, $"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _statusLabel.Text = "Failed to save settings.";
            _statusLabel.ForeColor = Color.FromArgb(220, 38, 38);
        }
    }
}
