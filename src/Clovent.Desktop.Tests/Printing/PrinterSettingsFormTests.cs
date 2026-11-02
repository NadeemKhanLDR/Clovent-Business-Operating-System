using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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
        form.ClientSize = new Size(width, height);
        form.PerformLayout();

        var grpProfiles = Assert.Single(form.Controls.Find("grpProfiles", searchAllChildren: true));
        var grpDetails = Assert.Single(form.Controls.Find("grpProfileDetails", searchAllChildren: true));

        // Group boxes must be side-by-side without horizontal overlap and within form bounds
        Assert.True(grpProfiles.Right <= grpDetails.Left, $"grpProfiles.Right ({grpProfiles.Right}) should be <= grpDetails.Left ({grpDetails.Left})");
        Assert.True(grpProfiles.Left >= 0 && grpProfiles.Top >= 0, "grpProfiles must be within form upper-left bounds");
        Assert.True(grpDetails.Right <= form.ClientSize.Width, $"grpDetails.Right ({grpDetails.Right}) must be within form ClientSize.Width ({form.ClientSize.Width})");

        // Helper: Convert control bounds to common form coordinate space
        Rectangle ToFormCoordinates(Control control)
        {
            int x = control.Left;
            int y = control.Top;
            var parent = control.Parent;
            while (parent != null && parent != form)
            {
                x += parent.Left;
                y += parent.Top;
                parent = parent.Parent;
            }
            return new Rectangle(x, y, control.Width, control.Height);
        }

        // 1. Group controls must not intersect in common form coordinate space
        var grpProfilesFormRect = ToFormCoordinates(grpProfiles);
        var grpDetailsFormRect = ToFormCoordinates(grpDetails);
        Assert.False(grpProfilesFormRect.IntersectsWith(grpDetailsFormRect), "grpProfiles and grpDetails must not overlap in form space");

        // 2. Pairwise intersection check across all visible controls in grpProfileDetails (common form space)
        var visibleDetailControls = grpDetails.Controls.Cast<Control>()
            .Where(c => c.Visible)
            .ToList();

        for (int i = 0; i < visibleDetailControls.Count; i++)
        {
            var c1 = visibleDetailControls[i];
            var r1 = ToFormCoordinates(c1);

            // Controls remain inside parent's usable bounds with readable positive dimensions
            Assert.True(c1.Width > 0 && c1.Height > 0, $"Control '{c1.Name}' must have positive rendered dimensions");
            Assert.True(c1.Left >= 0, $"Control '{c1.Name}' Left ({c1.Left}) must be >= 0");
            Assert.True(c1.Right <= grpDetails.ClientSize.Width, $"Control '{c1.Name}' Right ({c1.Right}) exceeds parent ClientSize.Width ({grpDetails.ClientSize.Width})");
            Assert.True(c1.Bottom <= grpDetails.ClientSize.Height, $"Control '{c1.Name}' Bottom ({c1.Bottom}) exceeds parent ClientSize.Height ({grpDetails.ClientSize.Height})");

            for (int j = i + 1; j < visibleDetailControls.Count; j++)
            {
                var c2 = visibleDetailControls[j];
                var r2 = ToFormCoordinates(c2);

                Assert.False(r1.IntersectsWith(r2),
                    $"Collision detected between '{c1.Name}' (form bounds {r1}) and '{c2.Name}' (form bounds {r2})");
            }
        }

        // 3. Pairwise intersection check across all visible controls in grpProfiles (common form space)
        var visibleProfileControls = grpProfiles.Controls.Cast<Control>()
            .Where(c => c.Visible)
            .ToList();

        for (int i = 0; i < visibleProfileControls.Count; i++)
        {
            var c1 = visibleProfileControls[i];
            var r1 = ToFormCoordinates(c1);

            Assert.True(c1.Width > 0 && c1.Height > 0, $"Profile control '{c1.Name}' must have positive rendered dimensions");
            Assert.True(c1.Left >= 0, $"Profile control '{c1.Name}' Left ({c1.Left}) must be >= 0");
            Assert.True(c1.Right <= grpProfiles.ClientSize.Width, $"Profile control '{c1.Name}' Right ({c1.Right}) exceeds parent ClientSize.Width ({grpProfiles.ClientSize.Width})");
            Assert.True(c1.Bottom <= grpProfiles.ClientSize.Height, $"Profile control '{c1.Name}' Bottom ({c1.Bottom}) exceeds parent ClientSize.Height ({grpProfiles.ClientSize.Height})");

            for (int j = i + 1; j < visibleProfileControls.Count; j++)
            {
                var c2 = visibleProfileControls[j];
                var r2 = ToFormCoordinates(c2);

                Assert.False(r1.IntersectsWith(r2),
                    $"Collision in profiles panel between '{c1.Name}' (form bounds {r1}) and '{c2.Name}' (form bounds {r2})");
            }
        }

        // 4. Two-column layout semantic assertions:
        var lblProfileName = Assert.Single(form.Controls.Find("lblProfileName", searchAllChildren: true));
        var txtProfileName = Assert.Single(form.Controls.Find("txtProfileName", searchAllChildren: true));
        var lblQueue = Assert.Single(form.Controls.Find("lblQueue", searchAllChildren: true));
        var txtQueue = Assert.Single(form.Controls.Find("txtQueueName", searchAllChildren: true));
        var lblRole = Assert.Single(form.Controls.Find("lblRole", searchAllChildren: true));
        var cmbRole = Assert.Single(form.Controls.Find("cmbRole", searchAllChildren: true));
        var lblPaperWidth = Assert.Single(form.Controls.Find("lblPaperWidth", searchAllChildren: true));
        var cmbPaperWidth = Assert.Single(form.Controls.Find("cmbPaperWidth", searchAllChildren: true));
        var lblColumns = Assert.Single(form.Controls.Find("lblColumns", searchAllChildren: true));
        var spinColumns = Assert.Single(form.Controls.Find("spinColumns", searchAllChildren: true));

        // Label/editor pairs horizontal alignment and separation (label strictly to the left of editor)
        Assert.True(lblProfileName.Right <= txtProfileName.Left, "lblProfileName must not overlap txtProfileName horizontally");
        Assert.True(lblQueue.Right <= txtQueue.Left, "lblQueue must not overlap txtQueueName horizontally");
        Assert.True(lblRole.Right <= cmbRole.Left, "lblRole must not overlap cmbRole horizontally");
        Assert.True(lblPaperWidth.Right <= cmbPaperWidth.Left, "lblPaperWidth must not overlap cmbPaperWidth horizontally");
        Assert.True(lblColumns.Right <= spinColumns.Left, "lblColumns must not overlap spinColumns horizontally");

        // Column separation: Column 1 controls must be to the left of Column 2 controls with positive margin
        Assert.True(txtProfileName.Right <= lblQueue.Left, "Row 1: txtProfileName must be to the left of lblQueue with suitable column spacing");
        Assert.True(cmbRole.Right <= lblPaperWidth.Left, "Row 2: cmbRole must be to the left of lblPaperWidth with suitable column spacing");

        // Vertical row progression: Row 1 above Row 2, Row 2 above Row 3
        Assert.True(lblProfileName.Bottom <= lblRole.Top, $"Column 1: lblProfileName.Bottom ({lblProfileName.Bottom}) must be <= lblRole.Top ({lblRole.Top})");
        Assert.True(lblRole.Bottom <= lblColumns.Top, $"Column 1: lblRole.Bottom ({lblRole.Bottom}) must be <= lblColumns.Top ({lblColumns.Top})");
        Assert.True(lblQueue.Bottom <= lblPaperWidth.Top, $"Column 2: lblQueue.Bottom ({lblQueue.Bottom}) must be <= lblPaperWidth.Top ({lblPaperWidth.Top})");

        // Profiles list action buttons: side-by-side along bottom without horizontal collision
        var btnAdd = Assert.Single(form.Controls.Find("btnAddProfile", searchAllChildren: true));
        var btnDelete = Assert.Single(form.Controls.Find("btnDeleteProfile", searchAllChildren: true));
        var btnSetDefault = Assert.Single(form.Controls.Find("btnSetDefault", searchAllChildren: true));
        var lstProfiles = Assert.Single(form.Controls.Find("lstProfiles", searchAllChildren: true));

        Assert.True(lstProfiles.Bottom <= btnAdd.Top, "lstProfiles must be above action buttons with positive spacing");
        Assert.True(btnAdd.Right <= btnDelete.Left, "btnAddProfile must be to the left of btnDeleteProfile");
        Assert.True(btnDelete.Right <= btnSetDefault.Left, "btnDeleteProfile must be to the left of btnSetDefault");
    }
}
