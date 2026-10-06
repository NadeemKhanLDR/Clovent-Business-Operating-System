using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Licensing;
using Clovent.Desktop.Restaurant;
using DevExpress.XtraEditors;
using Xunit;

namespace Clovent.Desktop.Tests.Licensing;

/// <summary>
/// Targeted High-DPI responsive layout tests for SoftwareRegistrationForm
/// and OperationsHealthForm across 100%, 125%, 150%, 175%, 200%, 225%, and 250% scaling.
/// Verifies no overlapping labels/values, minimum dimensions, button boundaries,
/// and full-width hardware ID display per Part A2 and Part A5 requirements.
/// </summary>
public sealed class SoftwareRegistrationFormLayoutTests
{
    [Fact]
    public void SoftwareRegistrationForm_CanConstructParameterlessWithoutExceptions()
    {
        using var form = new SoftwareRegistrationForm();
        Assert.NotNull(form);
        Assert.True(form.MinimumSize.Width >= 560, "Minimum width must be at least 560px.");
        Assert.True(form.MinimumSize.Height >= 460, "Minimum height must be at least 460px.");
    }

    [Theory]
    [InlineData(1.0f)] // 100% DPI (96 DPI)
    [InlineData(1.25f)] // 125% DPI (120 DPI)
    [InlineData(1.5f)] // 150% DPI (144 DPI)
    [InlineData(1.75f)] // 175% DPI (168 DPI)
    [InlineData(2.0f)] // 200% DPI (192 DPI)
    [InlineData(2.25f)] // 225% DPI (216 DPI)
    [InlineData(2.5f)] // 250% DPI (240 DPI)
    public void SoftwareRegistrationForm_LayoutInvariants_SatisfiedAtAllDpiScales(float scale)
    {
        using var form = new SoftwareRegistrationForm();

        // Simulate form size at this DPI scale
        int width = (int)(680 * scale);
        int height = (int)(530 * scale);
        form.Size = new Size(width, height);

        // Invoke private ApplyResponsiveLayout method
        var applyMethod = typeof(SoftwareRegistrationForm).GetMethod(
            "ApplyResponsiveLayout",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(applyMethod);
        applyMethod.Invoke(form, null);

        // Retrieve controls via reflection
        var lblStatus = GetField<LabelControl>(form, "lblStatus");
        var lblProductTitle = GetField<LabelControl>(form, "lblProductTitle");
        var lblProductVal = GetField<LabelControl>(form, "lblProductVal");
        var lblVersionTitle = GetField<LabelControl>(form, "lblVersionTitle");
        var lblVersionVal = GetField<LabelControl>(form, "lblVersionVal");
        var lblLicensedToTitle = GetField<LabelControl>(form, "lblLicensedToTitle");
        var lblLicensedToVal = GetField<LabelControl>(form, "lblLicensedToVal");
        var lblLicenseTypeTitle = GetField<LabelControl>(form, "lblLicenseTypeTitle");
        var lblLicenseTypeVal = GetField<LabelControl>(form, "lblLicenseTypeVal");
        var lblMachineIdTitle = GetField<LabelControl>(form, "lblMachineIdTitle");
        var txtMachineId = GetField<TextEdit>(form, "txtMachineId");
        var btnCopyMachineId = GetField<SimpleButton>(form, "btnCopyMachineId");
        var btnImport = GetField<SimpleButton>(form, "btnImport");
        var btnClose = GetField<SimpleButton>(form, "btnClose");
        var panelBottom = GetField<Control>(form, "panelBottom");

        // 1. Status banner has positive width and fits inside client area
        Assert.True(lblStatus.Width >= 300, $"Status banner width ({lblStatus.Width}) must be >= 300px.");
        Assert.True(lblStatus.Right <= form.ClientSize.Width, "Status banner must not exceed client width.");

        // 2. Metadata rows: title and value do NOT overlap
        (LabelControl title, LabelControl val)[] rows =
        [
            (lblProductTitle, lblProductVal),
            (lblVersionTitle, lblVersionVal),
            (lblLicensedToTitle, lblLicensedToVal),
            (lblLicenseTypeTitle, lblLicenseTypeVal)
        ];

        foreach (var (title, val) in rows)
        {
            Assert.True(title.Right <= val.Left,
                $"Title '{title.Text}' (Right={title.Right}) overlaps or collides with value (Left={val.Left}) at scale {scale}x.");
            Assert.True(val.Right <= form.ClientSize.Width,
                $"Value '{val.Name}' exceeds form client width at scale {scale}x.");
        }

        // 3. Hardware ID editor has ample room and does not collide with copy button
        Assert.True(txtMachineId.Width >= 160, $"Hardware ID editor width ({txtMachineId.Width}) is too small.");
        Assert.True(txtMachineId.Right <= btnCopyMachineId.Left,
            $"Machine ID editor (Right={txtMachineId.Right}) collides with Copy button (Left={btnCopyMachineId.Left}).");
        Assert.True(btnCopyMachineId.Right <= form.ClientSize.Width,
            "Copy Machine ID button exceeds form client width.");

        // 4. Action buttons in bottom panel are within panel bounds
        Assert.True(btnImport.Left >= 0 && btnImport.Right <= panelBottom.ClientSize.Width,
            "Import button must be within bottom panel bounds.");
        Assert.True(btnClose.Left >= 0 && btnClose.Right <= panelBottom.ClientSize.Width,
            "Close button must be within bottom panel bounds.");
        Assert.True(btnImport.Width >= 100, $"Import button width ({btnImport.Width}) must be >= 100px.");
        Assert.True(btnClose.Width >= 80, $"Close button width ({btnClose.Width}) must be >= 80px.");
    }

    [Theory]
    [InlineData(1366, 768)]
    [InlineData(1600, 900)]
    [InlineData(1920, 1080)]
    public void SoftwareRegistrationForm_RepresentativeResolutions_FitsComfortably(int resW, int resH)
    {
        using var form = new SoftwareRegistrationForm();

        // Form default size should fit comfortably within screen resolution
        Assert.True(form.Width < resW, $"Form width {form.Width} exceeds display width {resW}.");
        Assert.True(form.Height < resH, $"Form height {form.Height} exceeds display height {resH}.");
    }

    [Fact]
    public void OperationsHealthForm_CanConstructParameterlessWithoutExceptions()
    {
        using var form = new OperationsHealthForm();
        Assert.NotNull(form);
        Assert.True(form.MinimumSize.Width >= 880, "OperationsHealthForm min width must be >= 880px.");
        Assert.True(form.MinimumSize.Height >= 600, "OperationsHealthForm min height must be >= 600px.");
    }

    [Fact]
    public void OperationsHealthForm_FontIsDpiReadable_NotDefaultWinForms8Pt()
    {
        using var form = new OperationsHealthForm();
        // Base font must be at least 9.5pt, not the tiny WinForms default 8.25pt
        Assert.True(form.Font.SizeInPoints >= 9.5F,
            $"OperationsHealthForm font size {form.Font.SizeInPoints}pt is too small; must be >= 9.5pt.");
    }

    private static T GetField<T>(object instance, string fieldName) where T : class
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(field);
        var val = field.GetValue(instance) as T;
        Assert.NotNull(val);
        return val;
    }
}
