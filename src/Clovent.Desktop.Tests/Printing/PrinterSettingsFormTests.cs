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

        // Verify height does not exceed 450px (scaled proportionally when running on High-DPI displays)
        var maxAllowedHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(450, form);
        var maxAllowedMinHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(400, form);
        Assert.True(form.Height <= maxAllowedHeight, $"PrinterSettingsForm height {form.Height} should be <= {maxAllowedHeight}px");
        Assert.True(form.MinimumSize.Height <= maxAllowedMinHeight, $"PrinterSettingsForm minimum height {form.MinimumSize.Height} should be <= {maxAllowedMinHeight}px");
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
}
