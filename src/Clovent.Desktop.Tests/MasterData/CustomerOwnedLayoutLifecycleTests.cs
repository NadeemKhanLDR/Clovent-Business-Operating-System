using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Localization;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Restaurant.Customers;
using Xunit;

namespace Clovent.Desktop.Tests.MasterData;

public sealed class CustomerOwnedLayoutLifecycleTests
{
    [Fact]
    public void OwnedPanel_WorksWithBaseSizingLocalizationAndRecursiveDisposal()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var previous = CultureInfo.CurrentUICulture;
            try
            {
                using var form = new CustomerEditForm("Edit Customer", code: "SYNTHETIC", name: "Test", email: "test@example.invalid");
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                var table = (TableLayoutPanel)typeof(MasterDataEditFormBase).GetField("_contentPanel", flags)!.GetValue(form)!;
                var opening = (Control)typeof(CustomerEditForm).GetField("_openingBalanceEdit", flags)!.GetValue(form)!;
                typeof(MasterDataEditFormBase).GetMethod("SetFixedRowHeight", flags)!.Invoke(form, [opening, 40]);
                Assert.Equal(SizeType.Absolute, table.RowStyles[7].SizeType);
                Assert.Equal(DesktopDpi.Scale(40, form), table.RowStyles[7].Height);
                var tree = Descendants(form).ToArray();
                var parents = tree.ToDictionary(c => c, c => c.Parent);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ur-PK");
                LocalizationHelper.LocalizeControl(form);
                Assert.All(tree, c => Assert.Same(parents[c], c.Parent));
                Assert.Equal("SYNTHETIC", form.CodeValue);
                Assert.Equal("test@example.invalid", form.EmailValue);
                Assert.Equal(4, table.ColumnCount);
                Assert.Equal(2, form.Controls.Count);
                Assert.False((bool)typeof(CustomerEditForm).GetProperty("PersistWindowPlacement", flags)!.GetValue(form)!);
                Assert.False((bool)typeof(CustomerEditForm).GetProperty("AutoComputeClientSize", flags)!.GetValue(form)!);
                Assert.False(form.Visible);
                form.Dispose(); form.Dispose();
                Assert.All(tree, c => Assert.True(c.IsDisposed));
            }
            catch (Exception ex) { failure = ex; }
            finally { CultureInfo.CurrentUICulture = previous; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(45)));
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>()
        .SelectMany(c => new[] { c }.Concat(Descendants(c)));
}
