using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Clovent.Desktop.Commissioning.UI;
using DevExpress.XtraEditors;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class WizardDesignerCompositionTests
{
    [Theory]
    [InlineData(960, 640)]
    [InlineData(1280, 800)]
    public void AllPages_HaveNativeWrappingLayout_WithoutRunningCommissioning(int width, int height)
    {
        RunSta(() =>
        {
            using var form = Assert.IsType<FirstRunWizardForm>(LicenseManager.CreateWithContext(
                typeof(FirstRunWizardForm), new DesigntimeLicenseContext()));
            form.ClientSize = new Size(width, height);
            form.PerformLayout();
            Assert.False(form.Visible);
            for (var step = 1; step <= 8; step++)
            {
                var panel = Field<Panel>(form, $"panelStep{step}");
                var layout = Assert.Single(panel.Controls.OfType<TableLayoutPanel>());
                Assert.True(panel.AutoScroll);
                Assert.True(layout.AutoSize);
                Assert.Equal(DockStyle.Top, layout.Dock);
                Assert.Equal(1, layout.ColumnCount);
                Assert.Equal(SizeType.Percent, layout.ColumnStyles[0].SizeType);
                Assert.Contains(Field<LabelControl>(form, $"lblStep{step}Title"), layout.Controls.Cast<Control>());
                var description = Field<LabelControl>(form, $"lblStep{step}Desc");
                Assert.Same(layout, description.Parent);
                Assert.Equal(LabelAutoSizeMode.Vertical, description.AutoSizeMode);
                var rows = layout.Controls.Cast<Control>().Select(c => layout.GetRow(c)).ToArray();
                Assert.Equal(rows.Length, rows.Distinct().Count());
            }
            foreach (var name in new[] { "btnCancel", "btnBack", "btnNext", "btnFinish" })
            {
                var button = Field<SimpleButton>(form, name);
                Assert.True(button.AutoSize);
                Assert.True(button.MinimumSize.Height >= 30);
                Assert.IsType<TableLayoutPanel>(button.Parent);
            }
            var welcome = Field<TableLayoutPanel>(form, "step1Layout");
            welcome.PerformLayout();
            Assert.True(Field<LabelControl>(form, "lblStep1Desc").Bottom <= Field<GroupControl>(form, "grpPrerequisites").Top);
            Assert.False(form.Visible);
        });
    }

    private static T Field<T>(object value, string name) => (T)value.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(value)!;

    internal static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { error = ex; } }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(45)), "Hidden layout check timed out.");
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
