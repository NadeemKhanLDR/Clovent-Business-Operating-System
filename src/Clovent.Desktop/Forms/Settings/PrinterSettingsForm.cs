using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Printing;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Platform.Printing;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Forms.Settings;

/// <summary>
/// Administrative dialog for configuring logical printer profiles, paper roll widths,
/// document role assignments, per-terminal customer &amp; merchant copy destinations,
/// and executing authorized diagnostic test prints.
/// Strictly Designer-loadable and DPI-aware under PerMonitorV2.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public partial class PrinterSettingsForm : XtraForm
{
    private readonly PrinterManagementService? _service;
    private PrinterConfiguration _configuration = new();
    private PrinterProfile? _selectedProfile;
    private readonly List<PrinterProfileOption> _profileOptions = [];

    private sealed record PrinterProfileOption(Guid? Id, string DisplayName);

    /// <summary>Parameterless constructor for Visual Studio WinForms Designer.</summary>
    public PrinterSettingsForm() : this(null)
    {
    }

    /// <summary>Constructs the form with the specified printer management coordinator service.</summary>
    public PrinterSettingsForm(PrinterManagementService? service)
    {
        InitializeComponent();

        _service = service;

        if (DesignModeHelper.IsInDesignMode)
        {
            return;
        }

        Load += PrinterSettingsForm_Load;
        btnRefreshQueues.Click += (s, e) => LoadSystemQueues();
        cmbInstalledPrinters.SelectedIndexChanged += CmbInstalledPrinters_SelectedIndexChanged;
        lstProfiles.SelectedIndexChanged += LstProfiles_SelectedIndexChanged;
        btnAddProfile.Click += BtnAddProfile_Click;
        btnSaveProfile.Click += BtnSaveProfile_Click;
        btnDeleteProfile.Click += BtnDeleteProfile_Click;
        btnSetDefault.Click += BtnSetDefault_Click;
        btnSetTerminalDefault.Click += BtnSetTerminalDefault_Click;
        btnPreviewTest.Click += BtnPreviewTest_Click;
        btnTestPrint.Click += BtnTestPrint_Click;

        btnBrowseLogo.Click += BtnBrowseLogo_Click;
        btnPreviewCustomer.Click += BtnPreviewCustomer_Click;
        btnPreviewMerchant.Click += BtnPreviewMerchant_Click;
        btnSaveTerminalSettings.Click += BtnSaveTerminalSettings_Click;
    }

    private async void PrinterSettingsForm_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
        {
            return;
        }

        ApplyDpiScaling();
        PopulateDropdowns();
        LoadSystemQueues();
        UpdateScopeDisplay();

        if (_service != null)
        {
            try
            {
                _configuration = await _service.GetConfigurationAsync();
                RefreshProfileList();
                LoadTerminalReceiptSettings();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error loading configuration: {ex.Message}";
            }
        }
        else
        {
            LoadTerminalReceiptSettings();
        }
    }

    private void ApplyDpiScaling()
    {
        ClientSize = new Size(
            DesktopDpi.Scale(840, this),
            DesktopDpi.Scale(690, this));
        MinimumSize = ClientSize;
    }

    private void PopulateDropdowns()
    {
        cmbRole.Properties.Items.Clear();
        foreach (var role in Enum.GetValues<PrinterRole>())
        {
            cmbRole.Properties.Items.Add(role);
        }
        cmbRole.SelectedIndex = 0;

        cmbPaperWidth.Properties.Items.Clear();
        cmbPaperWidth.Properties.Items.Add("80 mm Standard");
        cmbPaperWidth.Properties.Items.Add("58 mm Narrow");
        cmbPaperWidth.Properties.Items.Add("A4 Document");
        cmbPaperWidth.SelectedIndex = 0;

        cmbTerminalPaperWidth.Properties.Items.Clear();
        cmbTerminalPaperWidth.Properties.Items.Add("80 mm Standard");
        cmbTerminalPaperWidth.Properties.Items.Add("58 mm Narrow");
        cmbTerminalPaperWidth.Properties.Items.Add("A4 Document");
        cmbTerminalPaperWidth.SelectedIndex = 0;
    }

    private void UpdateScopeDisplay()
    {
        var terminalId = PosSettingsStore.LoadTerminalId();
        var branchId = PosSettingsStore.LoadBranchId();
        var termStr = terminalId.HasValue ? terminalId.Value.ToString() : "Local Workstation (Unbound)";
        var branchStr = branchId.HasValue ? branchId.Value.ToString() : "Default Store Branch";
        lblScopeInfo.Text = $"Scope: Terminal [{termStr}] | Branch [{branchStr}]";
    }

    private void LoadSystemQueues()
    {
        cmbInstalledPrinters.Properties.Items.Clear();

        if (_service != null)
        {
            var printers = _service.GetInstalledPrintersAsync().GetAwaiter().GetResult();
            foreach (var p in printers)
            {
                var label = p.IsDefault ? $"{p.Name} (Default)" : p.Name;
                cmbInstalledPrinters.Properties.Items.Add(label);
            }
        }
        else
        {
            var provider = new WindowsPrinterQueueProvider();
            foreach (var p in provider.GetInstalledPrinters())
            {
                var label = p.IsDefault ? $"{p.Name} (Default)" : p.Name;
                cmbInstalledPrinters.Properties.Items.Add(label);
            }
        }

        if (cmbInstalledPrinters.Properties.Items.Count > 0)
        {
            cmbInstalledPrinters.SelectedIndex = 0;
            lblStatus.Text = $"Found {cmbInstalledPrinters.Properties.Items.Count} print queues.";
        }
        else
        {
            lblCapabilityInfo.Text = "Hardware Capability: No Windows print queues detected.";
            lblStatus.Text = "No Windows print queues detected.";
        }
    }

    private void CmbInstalledPrinters_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbInstalledPrinters.SelectedItem is string selected)
        {
            var cleanName = selected.Replace(" (Default)", string.Empty);
            txtQueueName.Text = cleanName;

            var capability = BlackCopper85Detector.Inspect(cleanName);
            lblCapabilityInfo.Text = $"Hardware Capability: {capability.ModelDescription}";

            // Clearly disable unsupported options on virtual or document queues
            chkCutter.Enabled = capability.IsThermalReceipt;
            chkDrawer.Enabled = capability.IsThermalReceipt;

            if (_selectedProfile == null)
            {
                txtProfileName.Text = capability.IsBlackCopper85 ? "Black Copper 85" : cleanName;
                chkCutter.Checked = capability.SupportsCutter;
                chkDrawer.Checked = capability.SupportsCashDrawer;
                cmbPaperWidth.SelectedIndex = capability.RecommendedPaperWidth switch
                {
                    PaperWidth.Width58mm => 1,
                    PaperWidth.A4Custom => 2,
                    _ => 0
                };
                spinColumns.Value = capability.RecommendedColumns;
            }
        }
    }

    private void RefreshProfileList()
    {
        lstProfiles.Items.Clear();
        var terminalId = PosSettingsStore.LoadTerminalId();
        var terminalAssignment = terminalId.HasValue
            ? _configuration.Assignments.FirstOrDefault(a => a.TerminalId == terminalId.Value && a.Role == PrinterRole.Receipt)
            : null;

        foreach (var profile in _configuration.Profiles)
        {
            var isDef = profile.Id == _configuration.DefaultReceiptProfileId;
            var isTermDef = terminalAssignment != null && terminalAssignment.PrinterProfileId == profile.Id;

            var suffix = "";
            if (isDef && isTermDef) suffix = " [Global & Terminal Default]";
            else if (isTermDef) suffix = " [Terminal Default]";
            else if (isDef) suffix = " [Global Default]";

            lstProfiles.Items.Add($"{profile.ProfileName}{suffix}");
        }

        if (_configuration.Profiles.Count > 0 && lstProfiles.SelectedIndex < 0)
        {
            lstProfiles.SelectedIndex = 0;
        }

        PopulateTerminalProfileSelectors();
    }

    private void PopulateTerminalProfileSelectors()
    {
        _profileOptions.Clear();
        _profileOptions.Add(new PrinterProfileOption(null, "(Default Receipt Printer)"));
        foreach (var p in _configuration.Profiles)
        {
            _profileOptions.Add(new PrinterProfileOption(p.Id, p.ProfileName));
        }

        cmbTerminalDefaultReceipt.Properties.Items.Clear();
        cmbCustomerPrinter.Properties.Items.Clear();
        cmbMerchantPrinter.Properties.Items.Clear();

        foreach (var opt in _profileOptions)
        {
            if (opt.Id.HasValue)
            {
                cmbTerminalDefaultReceipt.Properties.Items.Add(opt.DisplayName);
            }
            cmbCustomerPrinter.Properties.Items.Add(opt.DisplayName);
            cmbMerchantPrinter.Properties.Items.Add(opt.DisplayName);
        }
    }

    private void LoadTerminalReceiptSettings()
    {
        var terminalId = PosSettingsStore.LoadTerminalId();
        var settings = _configuration.GetSettingsForTerminal(terminalId);

        // Terminal default printer
        var terminalAssignment = terminalId.HasValue
            ? _configuration.Assignments.FirstOrDefault(a => a.TerminalId == terminalId.Value && a.Role == PrinterRole.Receipt)
            : null;

        var defaultProfileId = terminalAssignment?.PrinterProfileId ?? _configuration.DefaultReceiptProfileId;
        if (defaultProfileId.HasValue)
        {
            var defaultProfile = _configuration.FindProfile(defaultProfileId.Value);
            if (defaultProfile != null)
            {
                cmbTerminalDefaultReceipt.SelectedItem = defaultProfile.ProfileName;
            }
        }
        else if (cmbTerminalDefaultReceipt.Properties.Items.Count > 0)
        {
            cmbTerminalDefaultReceipt.SelectedIndex = 0;
        }

        // Explicit validation notice for existing configurations above MaxCopyCount (do not silently accept)
        if ((settings.CustomerCopyEnabled && settings.CustomerCopyCount > TerminalReceiptSettings.MaxCopyCount) ||
            (settings.MerchantCopyEnabled && settings.MerchantCopyCount > TerminalReceiptSettings.MaxCopyCount))
        {
            XtraMessageBox.Show(
                $"The existing configuration for receipt copies exceeds the maximum supported limit of {TerminalReceiptSettings.MaxCopyCount} copies per transaction (Customer: {settings.CustomerCopyCount}, Merchant: {settings.MerchantCopyCount}).\n\nPlease correct the copy count to between {TerminalReceiptSettings.MinCopyCount} and {TerminalReceiptSettings.MaxCopyCount} before saving.",
                "Copy Count Configuration Requires Correction",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        // Customer copy
        chkCustomerCopy.Checked = settings.CustomerCopyEnabled;
        spinCustomerCopies.Value = Math.Clamp(settings.CustomerCopyCount, TerminalReceiptSettings.MinCopyCount, TerminalReceiptSettings.MaxCopyCount);
        SelectProfileInCombo(cmbCustomerPrinter, settings.CustomerCopyProfileId);

        // Merchant copy
        chkMerchantCopy.Checked = settings.MerchantCopyEnabled;
        spinMerchantCopies.Value = Math.Clamp(settings.MerchantCopyCount, TerminalReceiptSettings.MinCopyCount, TerminalReceiptSettings.MaxCopyCount);
        SelectProfileInCombo(cmbMerchantPrinter, settings.MerchantCopyProfileId);

        // Layout & formatting
        cmbTerminalPaperWidth.SelectedIndex = settings.PaperWidth switch
        {
            PaperWidth.Width58mm => 1,
            PaperWidth.A4Custom => 2,
            _ => 0
        };
        spinMarginMm.Value = Math.Clamp(settings.MarginMm, 0, 25);
        spinTerminalColumns.Value = Math.Clamp(settings.CharactersPerLine, 28, 120);
        chkTerminalCutter.Checked = settings.SupportsCutter;
        chkTerminalDrawer.Checked = settings.SupportsCashDrawer;

        txtHeaderText.Text = settings.HeaderText ?? string.Empty;
        txtFooterText.Text = settings.FooterText ?? string.Empty;
        chkEnableLogo.Checked = settings.EnableLogo;
        txtLogoPath.Text = settings.LogoPath ?? string.Empty;
    }

    private void SelectProfileInCombo(ComboBoxEdit combo, Guid? profileId)
    {
        if (profileId.HasValue)
        {
            var opt = _profileOptions.FirstOrDefault(o => o.Id == profileId.Value);
            if (opt != null)
            {
                combo.SelectedItem = opt.DisplayName;
                return;
            }
        }
        combo.SelectedIndex = 0; // Default
    }

    private Guid? GetSelectedProfileId(ComboBoxEdit combo)
    {
        if (combo.SelectedIndex < 0 || combo.SelectedItem is not string selectedText)
        {
            return null;
        }

        var match = _profileOptions.FirstOrDefault(o => o.DisplayName == selectedText);
        return match?.Id;
    }

    private void LstProfiles_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (lstProfiles.SelectedIndex >= 0 && lstProfiles.SelectedIndex < _configuration.Profiles.Count)
        {
            _selectedProfile = _configuration.Profiles[lstProfiles.SelectedIndex];
            LoadProfileToEditors(_selectedProfile);
        }
    }

    private void LoadProfileToEditors(PrinterProfile profile)
    {
        txtProfileName.Text = profile.ProfileName;
        txtQueueName.Text = profile.SystemPrinterName;
        cmbRole.SelectedItem = profile.Role;

        cmbPaperWidth.SelectedIndex = profile.PaperWidth switch
        {
            PaperWidth.Width58mm => 1,
            PaperWidth.A4Custom => 2,
            _ => 0
        };

        spinColumns.Value = profile.CharactersPerLine;
        spinCopies.Value = profile.PrintCopies;
        chkCutter.Checked = profile.SupportsCutter;
        chkDrawer.Checked = profile.SupportsCashDrawer;

        var capability = BlackCopper85Detector.Inspect(profile.SystemPrinterName);
        lblCapabilityInfo.Text = $"Hardware Capability: {capability.ModelDescription}";
        chkCutter.Enabled = capability.IsThermalReceipt;
        chkDrawer.Enabled = capability.IsThermalReceipt;

        lblStatus.Text = $"Selected profile: {profile.ProfileName}";
    }

    private void BtnAddProfile_Click(object? sender, EventArgs e)
    {
        _selectedProfile = null;
        var cleanQueue = cmbInstalledPrinters.SelectedItem?.ToString()?.Replace(" (Default)", string.Empty) ?? string.Empty;
        var cap = BlackCopper85Detector.Inspect(cleanQueue);

        txtProfileName.Text = cap.IsBlackCopper85 ? "Black Copper 85" : "New Receipt Printer";
        txtQueueName.Text = cleanQueue;
        cmbRole.SelectedItem = PrinterRole.Receipt;
        cmbPaperWidth.SelectedIndex = cap.RecommendedPaperWidth switch
        {
            PaperWidth.Width58mm => 1,
            PaperWidth.A4Custom => 2,
            _ => 0
        };
        spinColumns.Value = cap.RecommendedColumns;
        spinCopies.Value = 1;
        chkCutter.Checked = cap.SupportsCutter;
        chkDrawer.Checked = cap.SupportsCashDrawer;
        chkCutter.Enabled = cap.IsThermalReceipt;
        chkDrawer.Enabled = cap.IsThermalReceipt;

        txtProfileName.Focus();
        lblStatus.Text = "Enter details for new printer profile. Hardware capabilities remain Unverified until physically tested.";
    }

    private async void BtnSaveProfile_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtProfileName.Text))
        {
            XtraMessageBox.Show("Profile name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtQueueName.Text))
        {
            XtraMessageBox.Show("Windows printer queue name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var profile = _selectedProfile ?? new PrinterProfile { Id = Guid.NewGuid() };
        profile.ProfileName = txtProfileName.Text.Trim();
        profile.SystemPrinterName = txtQueueName.Text.Trim();
        profile.Role = cmbRole.SelectedItem is PrinterRole role ? role : PrinterRole.Receipt;

        profile.PaperWidth = cmbPaperWidth.SelectedIndex switch
        {
            1 => PaperWidth.Width58mm,
            2 => PaperWidth.A4Custom,
            _ => PaperWidth.Width80mm
        };

        profile.CharactersPerLine = (int)spinColumns.Value;
        profile.PrintCopies = (int)spinCopies.Value;
        profile.SupportsCutter = chkCutter.Checked;
        profile.SupportsCashDrawer = chkDrawer.Checked;

        // Clearly mark unsupported options on document printers
        if (profile.PaperWidth == PaperWidth.A4Custom && (profile.SupportsCutter || profile.SupportsCashDrawer))
        {
            profile.SupportsCutter = false;
            profile.SupportsCashDrawer = false;
            chkCutter.Checked = false;
            chkDrawer.Checked = false;
            XtraMessageBox.Show(
                "Hardware paper cutter and cash drawer kick commands are unsupported on A4 Document printers. These options have been disabled for this profile.",
                "Unsupported Hardware Options",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        if (_service != null)
        {
            try
            {
                await _service.SaveProfileAsync(profile);
                _configuration = await _service.GetConfigurationAsync();
                RefreshProfileList();
                lblStatus.Text = $"Profile '{profile.ProfileName}' saved successfully.";
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Failed to save profile: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void BtnDeleteProfile_Click(object? sender, EventArgs e)
    {
        if (_selectedProfile == null) return;

        var confirm = XtraMessageBox.Show(
            $"Are you sure you want to delete profile '{_selectedProfile.ProfileName}'?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm == DialogResult.Yes && _service != null)
        {
            try
            {
                await _service.DeleteProfileAsync(_selectedProfile.Id);
                _configuration = await _service.GetConfigurationAsync();
                _selectedProfile = null;
                RefreshProfileList();
                lblStatus.Text = "Profile deleted.";
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Failed to delete profile: {ex.Message}", "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void BtnSetDefault_Click(object? sender, EventArgs e)
    {
        if (_selectedProfile == null) return;

        if (_service != null)
        {
            try
            {
                await _service.SetDefaultReceiptPrinterAsync(_selectedProfile.Id);
                _configuration = await _service.GetConfigurationAsync();
                RefreshProfileList();
                lblStatus.Text = $"Profile '{_selectedProfile.ProfileName}' set as global default receipt printer.";
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Failed to set default printer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async void BtnSetTerminalDefault_Click(object? sender, EventArgs e)
    {
        if (_selectedProfile == null)
        {
            XtraMessageBox.Show("Please select a printer profile to assign to this terminal.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_service != null)
        {
            try
            {
                var terminalId = PosSettingsStore.LoadTerminalId() ?? Guid.NewGuid();
                PosSettingsStore.SaveTerminalId(terminalId);

                await _service.AssignTerminalDefaultReceiptPrinterAsync(
                    terminalId,
                    _selectedProfile.Id,
                    organizationId: null,
                    branchId: PosSettingsStore.LoadBranchId());

                _configuration = await _service.GetConfigurationAsync();
                RefreshProfileList();
                LoadTerminalReceiptSettings();
                lblStatus.Text = $"Profile '{_selectedProfile.ProfileName}' assigned as active terminal default receipt printer.";
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Failed to assign terminal default: {ex.Message}", "Assignment Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnBrowseLogo_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Title = "Select Store Logo Image",
            Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
            RestoreDirectory = true
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            txtLogoPath.Text = ofd.FileName;
            chkEnableLogo.Checked = true;
        }
    }

    private async void BtnSaveTerminalSettings_Click(object? sender, EventArgs e)
    {
        if (chkEnableLogo.Checked && !string.IsNullOrWhiteSpace(txtLogoPath.Text) && !File.Exists(txtLogoPath.Text))
        {
            var warn = XtraMessageBox.Show(
                $"The specified logo file does not exist on disk:\n\n{txtLogoPath.Text}\n\nDo you want to save anyway?",
                "Logo File Not Found",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (warn != DialogResult.Yes)
            {
                return;
            }
        }

        if (chkCustomerCopy.Checked && (spinCustomerCopies.Value < TerminalReceiptSettings.MinCopyCount || spinCustomerCopies.Value > TerminalReceiptSettings.MaxCopyCount))
        {
            XtraMessageBox.Show(
                $"Customer copy count ({spinCustomerCopies.Value}) is invalid. Supported range is {TerminalReceiptSettings.MinCopyCount} to {TerminalReceiptSettings.MaxCopyCount} copies per transaction.",
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        if (chkMerchantCopy.Checked && (spinMerchantCopies.Value < TerminalReceiptSettings.MinCopyCount || spinMerchantCopies.Value > TerminalReceiptSettings.MaxCopyCount))
        {
            XtraMessageBox.Show(
                $"Merchant copy count ({spinMerchantCopies.Value}) is invalid. Supported range is {TerminalReceiptSettings.MinCopyCount} to {TerminalReceiptSettings.MaxCopyCount} copies per transaction.",
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var terminalId = PosSettingsStore.LoadTerminalId() ?? Guid.NewGuid();
        PosSettingsStore.SaveTerminalId(terminalId);

        var settings = new TerminalReceiptSettings
        {
            TerminalId = terminalId,
            CustomerCopyEnabled = chkCustomerCopy.Checked,
            CustomerCopyProfileId = GetSelectedProfileId(cmbCustomerPrinter),
            CustomerCopyCount = (int)spinCustomerCopies.Value,
            MerchantCopyEnabled = chkMerchantCopy.Checked,
            MerchantCopyProfileId = GetSelectedProfileId(cmbMerchantPrinter),
            MerchantCopyCount = (int)spinMerchantCopies.Value,
            PaperWidth = cmbTerminalPaperWidth.SelectedIndex switch
            {
                1 => PaperWidth.Width58mm,
                2 => PaperWidth.A4Custom,
                _ => PaperWidth.Width80mm
            },
            MarginMm = (int)spinMarginMm.Value,
            CharactersPerLine = (int)spinTerminalColumns.Value,
            SupportsCutter = chkTerminalCutter.Checked,
            SupportsCashDrawer = chkTerminalDrawer.Checked,
            HeaderText = txtHeaderText.Text.Trim(),
            FooterText = txtFooterText.Text.Trim(),
            EnableLogo = chkEnableLogo.Checked,
            LogoPath = string.IsNullOrWhiteSpace(txtLogoPath.Text) ? null : txtLogoPath.Text.Trim()
        };

        // Clearly mark unsupported options on document printers
        if (settings.PaperWidth == PaperWidth.A4Custom && (settings.SupportsCutter || settings.SupportsCashDrawer))
        {
            settings.SupportsCutter = false;
            settings.SupportsCashDrawer = false;
            chkTerminalCutter.Checked = false;
            chkTerminalDrawer.Checked = false;
            XtraMessageBox.Show(
                "Hardware paper cutter and cash drawer kick commands are unsupported on A4 Document printers. These options have been disabled for terminal receipt printing.",
                "Unsupported Hardware Options",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        if (_service != null)
        {
            try
            {
                await _service.SaveTerminalReceiptSettingsAsync(terminalId, settings);

                // Assign terminal default printer if specified
                var defaultProfileName = cmbTerminalDefaultReceipt.SelectedItem?.ToString();
                var defaultProfile = _configuration.Profiles.FirstOrDefault(p => p.ProfileName == defaultProfileName);
                if (defaultProfile != null)
                {
                    await _service.AssignTerminalDefaultReceiptPrinterAsync(
                        terminalId,
                        defaultProfile.Id,
                        organizationId: null,
                        branchId: PosSettingsStore.LoadBranchId());
                }

                _configuration = await _service.GetConfigurationAsync();
                RefreshProfileList();
                UpdateScopeDisplay();
                lblStatus.Text = "Terminal receipt settings and copy routing saved successfully.";
                XtraMessageBox.Show("Terminal receipt configuration saved successfully.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Failed to save terminal receipt settings: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        else
        {
            _configuration.SetSettingsForTerminal(terminalId, settings);
            lblStatus.Text = "Terminal receipt settings updated (offline mode).";
        }
    }

    private PrintableReceiptData BuildSampleReceiptData()
    {
        return new PrintableReceiptData(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-DEMO-001",
            DailySalesNumber: 42,
            OrderType: "DineIn",
            CompletedAtUtc: DateTimeOffset.Now,
            Items:
            [
                new("Angus Beef Burger", "BURGER-01", 1, 14.50m, 14.50m),
                new("Seasoned Curly Fries", "FRIES-01", 1, 5.50m, 5.50m),
                new("Iced Lemon Tea", "BEV-01", 2, 3.50m, 7.00m)
            ],
            Subtotal: 27.00m,
            TaxTotal: 2.16m,
            DiscountTotal: 0m,
            ServiceChargeTotal: 0m,
            GrandTotal: 29.16m,
            Balance: 0m,
            Payments: [new("Cash", 30.00m)],
            CashierName: "Alice Cashier",
            TerminalName: "Workstation Register 1",
            BranchName: "Downtown Branch",
            OrganizationName: "Clovent Operating System",
            TaxRegistrationNumber: "TAX-99887766",
            CustomerNotes: "Thank you for shopping local!");
    }

    private void BtnPreviewCustomer_Click(object? sender, EventArgs e)
    {
        var sampleData = BuildSampleReceiptData();
        var options = new ReceiptRenderOptions(
            PaperWidth: cmbTerminalPaperWidth.SelectedIndex switch
            {
                1 => PaperWidth.Width58mm,
                2 => PaperWidth.A4Custom,
                _ => PaperWidth.Width80mm
            },
            CharactersPerLine: (int)spinTerminalColumns.Value,
            CopyType: ReceiptCopyType.Customer,
            MarginMm: (int)spinMarginMm.Value,
            HeaderText: string.IsNullOrWhiteSpace(txtHeaderText.Text) ? null : txtHeaderText.Text.Trim(),
            FooterText: string.IsNullOrWhiteSpace(txtFooterText.Text) ? null : txtFooterText.Text.Trim(),
            LogoPath: txtLogoPath.Text.Trim(),
            EnableLogo: chkEnableLogo.Checked);

        var formatted = ReceiptSnapshotFormatter.Format(sampleData, options);
        using var previewForm = new ReceiptPreviewForm(formatted.FormattedText);
        previewForm.Text = "Customer Receipt Preview";
        previewForm.ShowDialog(this);
    }

    private void BtnPreviewMerchant_Click(object? sender, EventArgs e)
    {
        var sampleData = BuildSampleReceiptData();
        var options = new ReceiptRenderOptions(
            PaperWidth: cmbTerminalPaperWidth.SelectedIndex switch
            {
                1 => PaperWidth.Width58mm,
                2 => PaperWidth.A4Custom,
                _ => PaperWidth.Width80mm
            },
            CharactersPerLine: (int)spinTerminalColumns.Value,
            CopyType: ReceiptCopyType.Merchant,
            MarginMm: (int)spinMarginMm.Value,
            HeaderText: string.IsNullOrWhiteSpace(txtHeaderText.Text) ? null : txtHeaderText.Text.Trim(),
            FooterText: string.IsNullOrWhiteSpace(txtFooterText.Text) ? null : txtFooterText.Text.Trim(),
            LogoPath: txtLogoPath.Text.Trim(),
            EnableLogo: chkEnableLogo.Checked);

        var formatted = ReceiptSnapshotFormatter.Format(sampleData, options);
        using var previewForm = new ReceiptPreviewForm(formatted.FormattedText);
        previewForm.Text = "Merchant / Store Accounting Receipt Preview";
        previewForm.ShowDialog(this);
    }

    private void BtnPreviewTest_Click(object? sender, EventArgs e)
    {
        if (_selectedProfile == null)
        {
            XtraMessageBox.Show("Please select a printer profile to preview.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var slipText = ReceiptSnapshotFormatter.FormatTestPrintSlip(_selectedProfile, "Administrator");
        using var previewForm = new ReceiptPreviewForm(slipText);
        previewForm.ShowDialog(this);
    }

    private async void BtnTestPrint_Click(object? sender, EventArgs e)
    {
        if (_selectedProfile == null)
        {
            XtraMessageBox.Show("Please select a printer profile before testing.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = XtraMessageBox.Show(
            $"Are you sure you want to send a test print to physical queue '{_selectedProfile.SystemPrinterName}'?",
            "Authorize Hardware Test Print",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            lblStatus.Text = "Test print cancelled by operator.";
            return;
        }

        if (_service != null)
        {
            lblStatus.Text = "Sending test print to Windows spooler...";
            var result = await _service.ExecuteTestPrintAsync(_selectedProfile.Id, "Administrator");

            if (result.Success)
            {
                lblStatus.Text = $"Test print successfully submitted to spooler for '{_selectedProfile.SystemPrinterName}'.";
                XtraMessageBox.Show(
                    $"Test print accepted by Windows Print Spooler for '{_selectedProfile.SystemPrinterName}'.\n\nNote: Spooler acceptance confirms job submission to Windows. Please verify paper output physically on the device.",
                    "Spooler Accepted",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                lblStatus.Text = $"Test print failed: {result.ErrorMessage}";
                XtraMessageBox.Show(
                    $"Failed to print to '{_selectedProfile.SystemPrinterName}':\n\n{result.ErrorMessage}",
                    "Print Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
