using Clovent.Desktop.Forms.Settings;
using Clovent.Desktop.Printing;
using Clovent.Platform.Printing;
using DevExpress.XtraEditors;
using DevExpress.XtraTab;
using Xunit;

namespace Clovent.Desktop.Tests.Printing;

public class PrinterSettingsFormTests
{
    [Fact]
    public void Constructor_Parameterless_InitializesSuccessfullyForDesigner()
    {
        // Act & Assert: Parameterless constructor must succeed without throwing
        using var form = new PrinterSettingsForm();
        Assert.NotNull(form);
        Assert.Equal("Printer & Hardware Settings", form.Text);
        Assert.False(form.MaximizeBox);
        Assert.False(form.MinimizeBox);
    }

    [Fact]
    public void Controls_AreProperlyInstantiated()
    {
        using var form = new PrinterSettingsForm();

        // Verify key controls exist in control hierarchy
        Assert.NotEmpty(form.Controls);
        var title = form.Controls.Find("lblTitle", searchAllChildren: true);
        var bottomPanel = form.Controls.Find("panelBottom", searchAllChildren: true);
        var testButton = form.Controls.Find("btnTestPrint", searchAllChildren: true);
        var previewButton = form.Controls.Find("btnPreviewTest", searchAllChildren: true);
        var queuesGroup = form.Controls.Find("grpInstalled", searchAllChildren: true);

        Assert.NotEmpty(title);
        Assert.NotEmpty(bottomPanel);
        Assert.NotEmpty(testButton);
        Assert.NotEmpty(previewButton);
        Assert.NotEmpty(queuesGroup);
    }

    [Fact]
    public void TabControlAndTerminalReceiptControls_AreProperlyInstantiated()
    {
        using var form = new PrinterSettingsForm();

        var tabControl = form.Controls.Find("tabControl", searchAllChildren: true);
        Assert.NotEmpty(tabControl);
        var tabs = Assert.IsAssignableFrom<XtraTabControl>(tabControl[0]);
        Assert.Equal(2, tabs.TabPages.Count);
        Assert.Equal("Printers & Profiles", tabs.TabPages[0].Text);
        Assert.Equal("Terminal Receipt Setup & Copies", tabs.TabPages[1].Text);

        // Terminal scope & copy controls
        var chkCustomer = form.Controls.Find("chkCustomerCopy", searchAllChildren: true);
        var cmbCustomerPrinter = form.Controls.Find("cmbCustomerPrinter", searchAllChildren: true);
        var spinCustomerCopies = form.Controls.Find("spinCustomerCopies", searchAllChildren: true);
        var chkMerchant = form.Controls.Find("chkMerchantCopy", searchAllChildren: true);
        var cmbMerchantPrinter = form.Controls.Find("cmbMerchantPrinter", searchAllChildren: true);
        var spinMerchantCopies = form.Controls.Find("spinMerchantCopies", searchAllChildren: true);

        Assert.NotEmpty(chkCustomer);
        Assert.NotEmpty(cmbCustomerPrinter);
        Assert.NotEmpty(spinCustomerCopies);
        Assert.NotEmpty(chkMerchant);
        Assert.NotEmpty(cmbMerchantPrinter);
        Assert.NotEmpty(spinMerchantCopies);

        // Layout & branding controls
        var cmbPaperWidth = form.Controls.Find("cmbTerminalPaperWidth", searchAllChildren: true);
        var spinMargin = form.Controls.Find("spinMarginMm", searchAllChildren: true);
        var spinColumns = form.Controls.Find("spinTerminalColumns", searchAllChildren: true);
        var txtHeader = form.Controls.Find("txtHeaderText", searchAllChildren: true);
        var txtFooter = form.Controls.Find("txtFooterText", searchAllChildren: true);
        var chkLogo = form.Controls.Find("chkEnableLogo", searchAllChildren: true);
        var txtLogo = form.Controls.Find("txtLogoPath", searchAllChildren: true);

        Assert.NotEmpty(cmbPaperWidth);
        Assert.NotEmpty(spinMargin);
        Assert.NotEmpty(spinColumns);
        Assert.NotEmpty(txtHeader);
        Assert.NotEmpty(txtFooter);
        Assert.NotEmpty(chkLogo);
        Assert.NotEmpty(txtLogo);

        // Buttons
        var btnSaveTerminal = form.Controls.Find("btnSaveTerminalSettings", searchAllChildren: true);
        var btnPreviewCust = form.Controls.Find("btnPreviewCustomer", searchAllChildren: true);
        var btnPreviewMerch = form.Controls.Find("btnPreviewMerchant", searchAllChildren: true);
        var btnSetTermDef = form.Controls.Find("btnSetTerminalDefault", searchAllChildren: true);

        Assert.NotEmpty(btnSaveTerminal);
        Assert.NotEmpty(btnPreviewCust);
        Assert.NotEmpty(btnPreviewMerch);
        Assert.NotEmpty(btnSetTermDef);
    }

    [Theory]
    [InlineData("Black Copper BC-85AC Thermal Printer", true, false, false, PaperWidth.Width80mm, 42, "Unverified")]
    [InlineData("POS-80 Series Thermal Printer", false, false, false, PaperWidth.Width80mm, 42, "Unverified")]
    [InlineData("POS-58 Receipt Printer", false, false, false, PaperWidth.Width58mm, 32, "Unverified")]
    [InlineData("Microsoft Print to PDF", false, false, false, PaperWidth.A4Custom, 80, "Unsupported (Virtual Device)")]
    [InlineData("OneNote (Desktop)", false, false, false, PaperWidth.A4Custom, 80, "Unsupported (Virtual Device)")]
    public void BlackCopper85Detector_Inspect_DetectsCapabilitiesAccurately(
        string printerName,
        bool expectBlackCopper,
        bool expectCutter,
        bool expectDrawer,
        PaperWidth expectedWidth,
        int expectedColumns,
        string expectedStatus)
    {
        var profile = BlackCopper85Detector.Inspect(printerName);

        Assert.Equal(expectBlackCopper, profile.IsBlackCopper85);
        Assert.Equal(expectCutter, profile.SupportsCutter);
        Assert.Equal(expectDrawer, profile.SupportsCashDrawer);
        Assert.Equal(expectedWidth, profile.RecommendedPaperWidth);
        Assert.Equal(expectedColumns, profile.RecommendedColumns);
        Assert.Equal(expectedStatus, profile.HardwareVerificationStatus);
        Assert.Contains(expectedStatus == "Unverified" ? "Unverified" : "disabled", profile.ModelDescription);
    }

    [Theory]
    [InlineData(920, 720)]
    [InlineData(1440, 900)]
    [InlineData(2300, 1800)]
    public void ResponsiveLayout_ProfilesTab_HasNoCollisionsOrOverlaps(int width, int height)
    {
        using var form = new PrinterSettingsForm();
        form.ClientSize = new System.Drawing.Size(width, height);
        // Force layout pass via reflection or resizing
        form.Size = new System.Drawing.Size(width + 16, height + 39);

        var grpProfiles = Assert.Single(form.Controls.Find("grpProfiles", searchAllChildren: true));
        var grpDetails = Assert.Single(form.Controls.Find("grpProfileDetails", searchAllChildren: true));

        // Group boxes must be side-by-side without horizontal overlap
        Assert.True(grpProfiles.Right <= grpDetails.Left, $"grpProfiles.Right ({grpProfiles.Right}) should be <= grpDetails.Left ({grpDetails.Left})");

        var lblProfileName = Assert.Single(form.Controls.Find("lblProfileName", searchAllChildren: true));
        var txtProfileName = Assert.Single(form.Controls.Find("txtProfileName", searchAllChildren: true));
        var lblQueue = Assert.Single(form.Controls.Find("lblQueue", searchAllChildren: true));
        var txtQueue = Assert.Single(form.Controls.Find("txtQueueName", searchAllChildren: true));
        var lblRole = Assert.Single(form.Controls.Find("lblRole", searchAllChildren: true));
        var cmbRole = Assert.Single(form.Controls.Find("cmbRole", searchAllChildren: true));

        // Labels must not overlap their corresponding editors horizontally
        Assert.True(lblProfileName.Right <= txtProfileName.Left, "lblProfileName must not overlap txtProfileName");
        Assert.True(lblQueue.Right <= txtQueue.Left, "lblQueue must not overlap txtQueueName");
        Assert.True(lblRole.Right <= cmbRole.Left, "lblRole must not overlap cmbRole");

        // Vertical rows must have positive separation (no vertical collisions)
        Assert.True(lblProfileName.Bottom <= lblQueue.Top, $"lblProfileName.Bottom ({lblProfileName.Bottom}) must be <= lblQueue.Top ({lblQueue.Top})");
        Assert.True(lblQueue.Bottom <= lblRole.Top, $"lblQueue.Bottom ({lblQueue.Bottom}) must be <= lblRole.Top ({lblRole.Top})");

        // Profiles list action buttons must be stacked cleanly without overlap
        var btnAdd = Assert.Single(form.Controls.Find("btnAddProfile", searchAllChildren: true));
        var btnSetDefault = Assert.Single(form.Controls.Find("btnSetDefault", searchAllChildren: true));
        Assert.True(btnAdd.Bottom <= btnSetDefault.Top, "btnAddProfile must be above btnSetDefault without overlap");
    }
}
