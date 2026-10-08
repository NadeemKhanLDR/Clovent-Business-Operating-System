using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Printing;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Platform.Printing;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Forms.Settings;

/// <summary>
/// Administrative dialog for configuring logical printer profiles, paper roll widths,
/// document role assignments, and executing authorized diagnostic test prints.
/// Strictly Designer-loadable and DPI-aware under PerMonitorV2.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public partial class PrinterSettingsForm : XtraForm
{
    private readonly PrinterManagementService? _service;
    private PrinterConfiguration _configuration = new();
    private PrinterProfile? _selectedProfile;

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
        btnPreviewTest.Click += BtnPreviewTest_Click;
        btnTestPrint.Click += BtnTestPrint_Click;
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

        if (_service != null)
        {
            try
            {
                _configuration = await _service.GetConfigurationAsync();
                RefreshProfileList();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error loading configuration: {ex.Message}";
            }
        }
    }

    private void ApplyDpiScaling()
    {
        ClientSize = new Size(
            DesktopDpi.Scale(784, this),
            DesktopDpi.Scale(550, this));
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
            lblStatus.Text = "No Windows print queues detected.";
        }
    }

    private void CmbInstalledPrinters_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbInstalledPrinters.SelectedItem is string selected)
        {
            var cleanName = selected.Replace(" (Default)", string.Empty);
            txtQueueName.Text = cleanName;
            if (string.IsNullOrWhiteSpace(txtProfileName.Text))
            {
                txtProfileName.Text = cleanName;
            }
        }
    }

    private void RefreshProfileList()
    {
        lstProfiles.Items.Clear();
        foreach (var profile in _configuration.Profiles)
        {
            var isDef = profile.Id == _configuration.DefaultReceiptProfileId;
            var display = isDef ? $"* {profile.ProfileName} [Default]" : profile.ProfileName;
            lstProfiles.Items.Add(display);
        }

        if (_configuration.Profiles.Count > 0 && lstProfiles.SelectedIndex < 0)
        {
            lstProfiles.SelectedIndex = 0;
        }
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

        lblStatus.Text = $"Selected profile: {profile.ProfileName}";
    }

    private void BtnAddProfile_Click(object? sender, EventArgs e)
    {
        _selectedProfile = null;
        txtProfileName.Text = "New Receipt Printer";
        txtQueueName.Text = cmbInstalledPrinters.SelectedItem?.ToString()?.Replace(" (Default)", string.Empty) ?? string.Empty;
        cmbRole.SelectedItem = PrinterRole.Receipt;
        cmbPaperWidth.SelectedIndex = 0;
        spinColumns.Value = 42;
        spinCopies.Value = 1;
        chkCutter.Checked = true;
        chkDrawer.Checked = true;
        txtProfileName.Focus();
        lblStatus.Text = "Enter details for new printer profile.";
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
            await _service.DeleteProfileAsync(_selectedProfile.Id);
            _configuration = await _service.GetConfigurationAsync();
            _selectedProfile = null;
            RefreshProfileList();
            lblStatus.Text = "Profile deleted.";
        }
    }

    private async void BtnSetDefault_Click(object? sender, EventArgs e)
    {
        if (_selectedProfile == null) return;

        if (_service != null)
        {
            await _service.SetDefaultReceiptPrinterAsync(_selectedProfile.Id);
            _configuration = await _service.GetConfigurationAsync();
            RefreshProfileList();
            lblStatus.Text = $"Profile '{_selectedProfile.ProfileName}' set as default receipt printer.";
        }
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

        // Prompt invariant: Explicit authorization required before actuating hardware
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
            var result = await _service.ExecuteTestPrintAsync(_selectedProfile.Id, "Administrator", authorizedExplicitly: true);

            if (result.Success)
            {
                lblStatus.Text = $"Test print successfully submitted to spooler for '{_selectedProfile.SystemPrinterName}'.";
                XtraMessageBox.Show(
                    $"Test print accepted by Windows Print Spooler for '{_selectedProfile.SystemPrinterName}'.\n\nNote: Spooler acceptance confirms job submission. Please verify paper output physically on the device.",
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
