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
    public void DesignerInstance_CanBeDisposedWithoutRuntimeScope(Type type)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var view = Assert.IsAssignableFrom<Control>(Activator.CreateInstance(type));
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
