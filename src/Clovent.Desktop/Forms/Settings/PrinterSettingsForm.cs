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
public partial class PrinterSettingsForm : XtraForm
{
    private readonly PrinterManagementService? _service;
    private PrinterConfiguration _configuration = new();
    private PrinterProfile? _selectedProfile;
    private readonly List<PrinterProfileOption> _profileOptions = [];

    private sealed record PrinterProfileOption(Guid? Id, string DisplayName);

    /// <summary>Parameterless constructor for Visual Studio WinForms Designer.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
        Resize += (s, e) => ApplyResponsiveLayout();
        btnRefreshQueues.Click += BtnRefreshQueues_Click;
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
        var workArea = (Screen.FromControl(this) ?? Screen.PrimaryScreen)?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        int targetW = Math.Min(DesktopDpi.Scale(920, this), workArea.Width - 40);
        int targetH = Math.Min(DesktopDpi.Scale(720, this), workArea.Height - 60);
        ClientSize = new Size(targetW, targetH);
        MinimumSize = new Size(DesktopDpi.Scale(780, this), DesktopDpi.Scale(580, this));
        ApplyResponsiveLayout();
    }

    private void ApplyResponsiveLayout()
    {
        if (IsDisposed || ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        SuspendLayout();
        try
        {
            int padX = DesktopDpi.Scale(20, this);

            // Title & Subtitle
            lblTitle.Location = new Point(padX, DesktopDpi.Scale(12, this));
            lblSubtitle.Location = new Point(padX, lblTitle.Bottom + DesktopDpi.Scale(4, this));

            // Bottom Panel layout
            panelBottom.Height = DesktopDpi.Scale(50, this);
            int btnH = DesktopDpi.Scale(30, this);
            int btnY = (panelBottom.ClientSize.Height - btnH) / 2;
            int rightX = panelBottom.ClientSize.Width - padX;

            btnClose.Size = new Size(DesktopDpi.Scale(90, this), btnH);
            rightX -= btnClose.Width;
            btnClose.Location = new Point(rightX, btnY);

            btnTestPrint.Size = new Size(DesktopDpi.Scale(150, this), btnH);
            rightX -= (btnTestPrint.Width + DesktopDpi.Scale(10, this));
            btnTestPrint.Location = new Point(rightX, btnY);

            btnPreviewTest.Size = new Size(DesktopDpi.Scale(130, this), btnH);
            rightX -= (btnPreviewTest.Width + DesktopDpi.Scale(10, this));
            btnPreviewTest.Location = new Point(rightX, btnY);

            lblStatus.Location = new Point(padX, (panelBottom.ClientSize.Height - lblStatus.Height) / 2);
            lblStatus.Width = Math.Max(100, rightX - padX - DesktopDpi.Scale(10, this));

            // Top Group: grpInstalled
            int installedTop = lblSubtitle.Bottom + DesktopDpi.Scale(8, this);
            grpInstalled.Location = new Point(padX, installedTop);
            grpInstalled.Width = ClientSize.Width - (padX * 2);

            int grpPadX = DesktopDpi.Scale(14, this);
            int qBtnW = DesktopDpi.Scale(130, this);
            int qBtnH = DesktopDpi.Scale(26, this);
            btnRefreshQueues.Size = new Size(qBtnW, qBtnH);
            btnRefreshQueues.Location = new Point(grpInstalled.ClientSize.Width - grpPadX - qBtnW, DesktopDpi.Scale(24, this));

            lblInstalled.Location = new Point(grpPadX, btnRefreshQueues.Top + DesktopDpi.Scale(4, this));
            int cmbInstalledX = lblInstalled.Right + DesktopDpi.Scale(10, this);
            cmbInstalledPrinters.Location = new Point(cmbInstalledX, btnRefreshQueues.Top);
            cmbInstalledPrinters.Size = new Size(Math.Max(120, btnRefreshQueues.Left - cmbInstalledX - DesktopDpi.Scale(10, this)), qBtnH);

            lblCapabilityInfo.Location = new Point(grpPadX, btnRefreshQueues.Bottom + DesktopDpi.Scale(8, this));
            lblCapabilityInfo.Width = grpInstalled.ClientSize.Width - (grpPadX * 2);
            grpInstalled.Height = lblCapabilityInfo.Bottom + DesktopDpi.Scale(12, this);

            // TabControl
            int tabTop = grpInstalled.Bottom + DesktopDpi.Scale(10, this);
            int tabBottom = panelBottom.Top - DesktopDpi.Scale(10, this);
            tabControl.Location = new Point(padX, tabTop);
            tabControl.Size = new Size(ClientSize.Width - (padX * 2), Math.Max(200, tabBottom - tabTop));

            // Tab 1: tabProfiles
            LayoutProfilesTab();

            // Tab 2: tabTerminalReceipts
            LayoutTerminalReceiptsTab();
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    private void LayoutProfilesTab()
    {
        int pad = DesktopDpi.Scale(10, this);
        int pageW = tabProfiles.ClientSize.Width;
        int pageH = tabProfiles.ClientSize.Height;

        int listW = Math.Max(DesktopDpi.Scale(260, this), (int)(pageW * 0.32));
        grpProfiles.Location = new Point(pad, pad);
        grpProfiles.Size = new Size(listW, Math.Max(150, pageH - (pad * 2)));

        int pBtnH = DesktopDpi.Scale(28, this);
        int pPad = DesktopDpi.Scale(8, this);
        int halfW = (grpProfiles.ClientSize.Width - (pPad * 3)) / 2;

        int row2Y = grpProfiles.ClientSize.Height - pPad - pBtnH;
        int row1Y = row2Y - DesktopDpi.Scale(6, this) - pBtnH;

        btnAddProfile.Location = new Point(pPad, row1Y);
        btnAddProfile.Size = new Size(halfW, pBtnH);

        btnDeleteProfile.Location = new Point(btnAddProfile.Right + pPad, row1Y);
        btnDeleteProfile.Size = new Size(halfW, pBtnH);

        btnSetDefault.Location = new Point(pPad, row2Y);
        btnSetDefault.Size = new Size(halfW, pBtnH);

        btnSetTerminalDefault.Location = new Point(btnSetDefault.Right + pPad, row2Y);
        btnSetTerminalDefault.Size = new Size(halfW, pBtnH);

        lstProfiles.Location = new Point(pPad, DesktopDpi.Scale(26, this));
        lstProfiles.Size = new Size(grpProfiles.ClientSize.Width - (pPad * 2), Math.Max(50, row1Y - DesktopDpi.Scale(6, this) - lstProfiles.Top));

        int detX = grpProfiles.Right + pad;
        int detW = pageW - detX - pad;
        grpProfileDetails.Location = new Point(detX, pad);
        grpProfileDetails.Size = new Size(Math.Max(200, detW), Math.Max(150, pageH - (pad * 2)));

        int dPadX = DesktopDpi.Scale(14, this);
        int lblW = DesktopDpi.Scale(125, this);
        int editX = dPadX + lblW + DesktopDpi.Scale(8, this);
        int editW = Math.Max(140, grpProfileDetails.ClientSize.Width - editX - dPadX);
        int rowStep = DesktopDpi.Scale(36, this);
        int curY = DesktopDpi.Scale(30, this);

        lblProfileName.Location = new Point(dPadX, curY + DesktopDpi.Scale(4, this));
        txtProfileName.Location = new Point(editX, curY);
        txtProfileName.Size = new Size(editW, txtProfileName.Height);

        curY += rowStep;
        lblQueue.Location = new Point(dPadX, curY + DesktopDpi.Scale(4, this));
        txtQueueName.Location = new Point(editX, curY);
        txtQueueName.Size = new Size(editW, txtQueueName.Height);

        curY += rowStep;
        lblRole.Location = new Point(dPadX, curY + DesktopDpi.Scale(4, this));
        cmbRole.Location = new Point(editX, curY);
        cmbRole.Size = new Size(editW, cmbRole.Height);

        curY += rowStep;
        lblPaperWidth.Location = new Point(dPadX, curY + DesktopDpi.Scale(4, this));
        cmbPaperWidth.Location = new Point(editX, curY);
        cmbPaperWidth.Size = new Size(editW, cmbPaperWidth.Height);

        curY += rowStep;
        lblColumns.Location = new Point(dPadX, curY + DesktopDpi.Scale(4, this));
        int spinW = DesktopDpi.Scale(90, this);
        spinColumns.Location = new Point(editX, curY);
        spinColumns.Size = new Size(spinW, spinColumns.Height);

        int copiesLblX = spinColumns.Right + DesktopDpi.Scale(16, this);
        lblCopies.Location = new Point(copiesLblX, curY + DesktopDpi.Scale(4, this));
        spinCopies.Location = new Point(lblCopies.Right + DesktopDpi.Scale(8, this), curY);
        spinCopies.Size = new Size(spinW, spinCopies.Height);

        curY += rowStep;
        chkCutter.Location = new Point(editX, curY);
        chkCutter.Size = new Size(DesktopDpi.Scale(210, this), chkCutter.Height);
        chkDrawer.Location = new Point(chkCutter.Right + DesktopDpi.Scale(10, this), curY);
        chkDrawer.Size = new Size(DesktopDpi.Scale(210, this), chkDrawer.Height);

        curY += rowStep;
        lblHardwareNotes.Location = new Point(dPadX, curY);
        lblHardwareNotes.Size = new Size(grpProfileDetails.ClientSize.Width - (dPadX * 2), DesktopDpi.Scale(36, this));
        lblHardwareNotes.AutoSizeMode = LabelAutoSizeMode.Vertical;

        btnSaveProfile.Size = new Size(DesktopDpi.Scale(160, this), DesktopDpi.Scale(30, this));
        btnSaveProfile.Location = new Point(editX, grpProfileDetails.ClientSize.Height - dPadX - btnSaveProfile.Height);
    }

    private void LayoutTerminalReceiptsTab()
    {
        tabTerminalReceipts.AutoScroll = true;

        int pad = DesktopDpi.Scale(10, this);
        int pageW = tabTerminalReceipts.ClientSize.Width - (pad * 2);
        int curY = pad;

        grpScope.Location = new Point(pad, curY);
        grpScope.Width = pageW;

        int sPad = DesktopDpi.Scale(12, this);
        lblScopeInfo.Location = new Point(sPad, DesktopDpi.Scale(26, this));
        lblTerminalDefault.Location = new Point(sPad, lblScopeInfo.Bottom + DesktopDpi.Scale(8, this));
        cmbTerminalDefaultReceipt.Location = new Point(lblTerminalDefault.Right + DesktopDpi.Scale(10, this), lblTerminalDefault.Top - DesktopDpi.Scale(3, this));
        cmbTerminalDefaultReceipt.Size = new Size(DesktopDpi.Scale(260, this), cmbTerminalDefaultReceipt.Height);
        lblScopeNotice.Location = new Point(cmbTerminalDefaultReceipt.Right + DesktopDpi.Scale(10, this), lblTerminalDefault.Top);
        grpScope.Height = lblTerminalDefault.Bottom + DesktopDpi.Scale(14, this);

        curY = grpScope.Bottom + DesktopDpi.Scale(8, this);

        grpCopies.Location = new Point(pad, curY);
        grpCopies.Width = pageW;

        int cStep = DesktopDpi.Scale(34, this);
        int cY = DesktopDpi.Scale(26, this);

        chkCustomerCopy.Location = new Point(sPad, cY);
        chkCustomerCopy.Size = new Size(DesktopDpi.Scale(170, this), chkCustomerCopy.Height);
        lblCustomerPrinter.Location = new Point(chkCustomerCopy.Right + DesktopDpi.Scale(8, this), cY + DesktopDpi.Scale(3, this));
        cmbCustomerPrinter.Location = new Point(lblCustomerPrinter.Right + DesktopDpi.Scale(6, this), cY);
        cmbCustomerPrinter.Size = new Size(DesktopDpi.Scale(240, this), cmbCustomerPrinter.Height);
        lblCustomerCopies.Location = new Point(cmbCustomerPrinter.Right + DesktopDpi.Scale(10, this), cY + DesktopDpi.Scale(3, this));
        spinCustomerCopies.Location = new Point(lblCustomerCopies.Right + DesktopDpi.Scale(6, this), cY);
        spinCustomerCopies.Size = new Size(DesktopDpi.Scale(60, this), spinCustomerCopies.Height);

        cY += cStep;
        chkMerchantCopy.Location = new Point(sPad, cY);
        chkMerchantCopy.Size = new Size(DesktopDpi.Scale(170, this), chkMerchantCopy.Height);
        lblMerchantPrinter.Location = new Point(chkMerchantCopy.Right + DesktopDpi.Scale(8, this), cY + DesktopDpi.Scale(3, this));
        cmbMerchantPrinter.Location = new Point(lblMerchantPrinter.Right + DesktopDpi.Scale(6, this), cY);
        cmbMerchantPrinter.Size = new Size(DesktopDpi.Scale(240, this), cmbMerchantPrinter.Height);
        lblMerchantCopies.Location = new Point(cmbMerchantPrinter.Right + DesktopDpi.Scale(10, this), cY + DesktopDpi.Scale(3, this));
        spinMerchantCopies.Location = new Point(lblMerchantCopies.Right + DesktopDpi.Scale(6, this), cY);
        spinMerchantCopies.Size = new Size(DesktopDpi.Scale(60, this), spinMerchantCopies.Height);

        grpCopies.Height = chkMerchantCopy.Bottom + DesktopDpi.Scale(14, this);
        curY = grpCopies.Bottom + DesktopDpi.Scale(8, this);

        grpLayoutAndBranding.Location = new Point(pad, curY);
        grpLayoutAndBranding.Width = pageW;

        int bY = DesktopDpi.Scale(26, this);
        int bStep = DesktopDpi.Scale(34, this);

        lblTerminalPaperWidth.Location = new Point(sPad, bY + DesktopDpi.Scale(3, this));
        cmbTerminalPaperWidth.Location = new Point(lblTerminalPaperWidth.Right + DesktopDpi.Scale(6, this), bY);
        cmbTerminalPaperWidth.Size = new Size(DesktopDpi.Scale(150, this), cmbTerminalPaperWidth.Height);

        lblMarginMm.Location = new Point(cmbTerminalPaperWidth.Right + DesktopDpi.Scale(12, this), bY + DesktopDpi.Scale(3, this));
        spinMarginMm.Location = new Point(lblMarginMm.Right + DesktopDpi.Scale(6, this), bY);
        spinMarginMm.Size = new Size(DesktopDpi.Scale(60, this), spinMarginMm.Height);

        lblTerminalColumns.Location = new Point(spinMarginMm.Right + DesktopDpi.Scale(12, this), bY + DesktopDpi.Scale(3, this));
        spinTerminalColumns.Location = new Point(lblTerminalColumns.Right + DesktopDpi.Scale(6, this), bY);
        spinTerminalColumns.Size = new Size(DesktopDpi.Scale(70, this), spinTerminalColumns.Height);

        bY += bStep;
        chkTerminalCutter.Location = new Point(sPad, bY);
        chkTerminalCutter.Size = new Size(DesktopDpi.Scale(270, this), chkTerminalCutter.Height);
        chkTerminalDrawer.Location = new Point(chkTerminalCutter.Right + DesktopDpi.Scale(10, this), bY);
        chkTerminalDrawer.Size = new Size(DesktopDpi.Scale(330, this), chkTerminalDrawer.Height);

        bY += bStep;
        int tLblW = DesktopDpi.Scale(85, this);
        int tEditX = sPad + tLblW + DesktopDpi.Scale(6, this);
        int tEditW = grpLayoutAndBranding.ClientSize.Width - tEditX - sPad;

        lblHeaderText.Location = new Point(sPad, bY + DesktopDpi.Scale(3, this));
        txtHeaderText.Location = new Point(tEditX, bY);
        txtHeaderText.Size = new Size(tEditW, txtHeaderText.Height);

        bY += bStep;
        lblFooterText.Location = new Point(sPad, bY + DesktopDpi.Scale(3, this));
        txtFooterText.Location = new Point(tEditX, bY);
        txtFooterText.Size = new Size(tEditW, txtFooterText.Height);

        bY += bStep;
        chkEnableLogo.Location = new Point(sPad, bY);
        chkEnableLogo.Size = new Size(tLblW, chkEnableLogo.Height);
        btnBrowseLogo.Size = new Size(DesktopDpi.Scale(80, this), DesktopDpi.Scale(24, this));
        btnBrowseLogo.Location = new Point(grpLayoutAndBranding.ClientSize.Width - sPad - btnBrowseLogo.Width, bY);
        txtLogoPath.Location = new Point(tEditX, bY);
        txtLogoPath.Size = new Size(btnBrowseLogo.Left - tEditX - DesktopDpi.Scale(8, this), txtLogoPath.Height);

        grpLayoutAndBranding.Height = txtLogoPath.Bottom + DesktopDpi.Scale(14, this);
        curY = grpLayoutAndBranding.Bottom + DesktopDpi.Scale(10, this);

        int aBtnH = DesktopDpi.Scale(30, this);
        btnSaveTerminalSettings.Size = new Size(DesktopDpi.Scale(230, this), aBtnH);
        btnPreviewMerchant.Size = new Size(DesktopDpi.Scale(180, this), aBtnH);
        btnPreviewCustomer.Size = new Size(DesktopDpi.Scale(180, this), aBtnH);

        int btnRight = pageW + pad;
        btnSaveTerminalSettings.Location = new Point(btnRight - btnSaveTerminalSettings.Width, curY);
        btnPreviewMerchant.Location = new Point(btnSaveTerminalSettings.Left - DesktopDpi.Scale(10, this) - btnPreviewMerchant.Width, curY);
        btnPreviewCustomer.Location = new Point(btnPreviewMerchant.Left - DesktopDpi.Scale(10, this) - btnPreviewCustomer.Width, curY);
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

    private void BtnRefreshQueues_Click(object? sender, EventArgs e) => LoadSystemQueues();

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
