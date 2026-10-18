using Clovent.Desktop.Forms.Settings;
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
    public void IsUserControl_CompatibleWithTabbedViewDocumentManager()
    {
        using var form = new PrinterSettingsForm();

        // Must be a UserControl (not Form) so non-MDI DocumentManager.TabbedView does not throw NotSupportedException
        Assert.IsAssignableFrom<DevExpress.XtraEditors.XtraUserControl>(form);
        Assert.IsNotAssignableFrom<System.Windows.Forms.Form>(form);
    }

    [Fact]
    public void CompactDimensions_FitsWithinMdiTabWithoutExcessiveHeight()
    {
        using var form = new PrinterSettingsForm();

        // Verify height does not exceed 450px unscaled to prevent overflowing MDI tab viewport
        Assert.True(form.Height <= 450, $"PrinterSettingsForm height {form.Height} should be <= 450px");
        Assert.True(form.MinimumSize.Height <= 400, $"PrinterSettingsForm minimum height {form.MinimumSize.Height} should be <= 400px");
    }

    [Fact]
    public void ReceiptCopyControls_AreInstantiatedAndConfigured()
    {
        using var form = new PrinterSettingsForm();

        var chkCustomer = form.Controls.Find("chkCustomerCopy", searchAllChildren: true);
        var spinCustomer = form.Controls.Find("spinCustomerCopies", searchAllChildren: true);
        var chkMerchant = form.Controls.Find("chkMerchantCopy", searchAllChildren: true);
        var spinMerchant = form.Controls.Find("spinMerchantCopies", searchAllChildren: true);
        var cmbTrigger = form.Controls.Find("cmbMerchantTrigger", searchAllChildren: true);
        var chkSig = form.Controls.Find("chkMerchantSignature", searchAllChildren: true);

        Assert.NotEmpty(chkCustomer);
        Assert.NotEmpty(spinCustomer);
        Assert.NotEmpty(chkMerchant);
        Assert.NotEmpty(spinMerchant);
        Assert.NotEmpty(cmbTrigger);
        Assert.NotEmpty(chkSig);
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
