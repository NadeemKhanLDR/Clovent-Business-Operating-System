using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Clovent.Desktop.Restaurant.Reports;
using Xunit;

namespace Clovent.Desktop.Tests.MasterData;

public sealed class ReportDesignerLifecycleTests
{
    [Theory]
    [InlineData(typeof(TaxSummaryReportView))]
    [InlineData(typeof(RefundAuditReportView))]
    public void DesignerInstance_PreservesReportLayoutAndDisposesWithoutRuntimeScope(Type type)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var view = Assert.IsAssignableFrom<Control>(Activator.CreateInstance(type));
                var root = Assert.IsType<TableLayoutPanel>(Assert.Single(view.Controls.Cast<Control>()));
                Assert.Equal(3, root.RowCount);
                Assert.Equal(3, root.Controls.Count);
                var filters = Assert.IsType<FlowLayoutPanel>(Assert.Single(root.GetControlFromPosition(0, 0)!.Controls.Cast<Control>()));
                var kpis = Assert.IsType<FlowLayoutPanel>(Assert.Single(root.GetControlFromPosition(0, 1)!.Controls.Cast<Control>()));
                Assert.Equal(9, filters.Controls.Count);
                Assert.Equal(6, kpis.Controls.Count);
                var grid = Assert.IsType<DevExpress.XtraGrid.GridControl>(root.GetControlFromPosition(0, 2));
                var gridView = Assert.IsType<DevExpress.XtraGrid.Views.Grid.GridView>(grid.MainView);
                Assert.Equal(type == typeof(TaxSummaryReportView) ? 8 : 10, gridView.Columns.Count);
                Assert.False(gridView.OptionsBehavior.Editable);
                Assert.Equal("Warehouse:", filters.Controls[0].Text);
                Assert.Equal("Generate Report", filters.Controls[8].Text);
                foreach (var width in new[] { 800, 1000, 1280 })
                {
                    view.Size = new System.Drawing.Size(width, 700);
                    view.PerformLayout();
                    root.PerformLayout();
                    filters.PerformLayout();
                    Assert.True(filters.Controls[8].Bottom <= filters.ClientSize.Height,
                        "The wrapped Generate Report button must remain inside the filter panel.");
                }
                var period = Assert.IsType<DevExpress.XtraEditors.ComboBoxEdit>(filters.Controls[3]);
                period.Properties.Items.Add("Custom");
                var from = Assert.IsType<DevExpress.XtraEditors.DateEdit>(filters.Controls[5]);
                from.EditValue = new DateTime(2026, 1, 1);
                Assert.Equal("Custom", period.SelectedItem);
                view.Dispose();
                view.Dispose();
                Assert.True(view.IsDisposed);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Designer lifecycle test timed out.");
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
