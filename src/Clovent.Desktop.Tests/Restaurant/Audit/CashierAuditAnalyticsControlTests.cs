using System;
using Clovent.Desktop.Restaurant.Audit;
using Clovent.Desktop.Sessions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Audit;

public sealed class CashierAuditAnalyticsControlTests
{
    [Fact]
    public void DesignerConstructor_InitializesWithoutThrowing()
    {
        // Act & Assert
#pragma warning disable CS0618, CS0619 // Type or member is obsolete
        using var control = new CashierAuditAnalyticsControl();
#pragma warning restore CS0618, CS0619
        Assert.NotNull(control);
        Assert.Equal(System.Windows.Forms.DockStyle.Fill, control.Dock);
    }

    [Fact]
    public void RuntimeConstructor_InitializesControlsAndTabs()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IMediator>(new FakeMediator());

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var session = new FakeCurrentSession();

        // Act
        using var control = new CashierAuditAnalyticsControl(scopeFactory, session);

        // Assert
        Assert.NotNull(control);
        Assert.True(control.Controls.Count > 0);

        control.Size = new System.Drawing.Size(1024, 768);
        var form = new System.Windows.Forms.Form { Size = new System.Drawing.Size(1024, 768) };
        form.Controls.Add(control);
        form.Show();

        var refreshBtn = (DevExpress.XtraEditors.SimpleButton)typeof(CashierAuditAnalyticsControl)
            .GetField("_btnRefresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(control)!;
        var forecastBtn = (DevExpress.XtraEditors.SimpleButton)typeof(CashierAuditAnalyticsControl)
            .GetField("_btnRunForecast", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(control)!;
        var scanBtn = (DevExpress.XtraEditors.SimpleButton)typeof(CashierAuditAnalyticsControl)
            .GetField("_btnRunScan", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(control)!;
        var dtFrom = (DevExpress.XtraEditors.DateEdit)typeof(CashierAuditAnalyticsControl)
            .GetField("_dtFrom", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(control)!;
        var dtTo = (DevExpress.XtraEditors.DateEdit)typeof(CashierAuditAnalyticsControl)
            .GetField("_dtTo", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(control)!;

        // Verify buttons and date editors have adequate dimensions and do not clip
        Assert.True(refreshBtn.Height >= 28, $"Refresh button height ({refreshBtn.Height}) is too small");
        Assert.True(forecastBtn.Height >= 28, $"Forecast button height ({forecastBtn.Height}) is too small");
        Assert.True(scanBtn.Height >= 28, $"Scan button height ({scanBtn.Height}) is too small");

        Assert.True(dtFrom.Width >= 120, $"dtFrom width ({dtFrom.Width}) must be >= 120 to prevent year clipping");
        Assert.True(dtTo.Width >= 120, $"dtTo width ({dtTo.Width}) must be >= 120 to prevent year clipping");

        // Verify Forecast button is fully within the 1024 window and not pushed off screen
        var forecastRight = forecastBtn.PointToScreen(new System.Drawing.Point(forecastBtn.Width, 0)).X;
        var formRight = form.PointToScreen(new System.Drawing.Point(form.ClientSize.Width, 0)).X;
        Assert.True(forecastRight <= formRight, $"Forecast button right edge ({forecastRight}) overflows form right edge ({formRight})");
    }

    [Fact]
    public void RenderToBitmap_VisualChecks_1024_1366_1920()
    {
        Exception? error = null;
        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
                services.AddSingleton<IMediator>(new FakeMediator());

                var sp = services.BuildServiceProvider();
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var session = new FakeCurrentSession();

                var outDir = @"C:\Users\NexGen\.gemini\antigravity\brain\3c7e0d7f-7594-4af6-94cd-6cddb9366839";

                foreach (var size in new[] { new System.Drawing.Size(1024, 768), new System.Drawing.Size(1366, 768), new System.Drawing.Size(1920, 1080) })
                {
                    using var control = new CashierAuditAnalyticsControl(scopeFactory, session);
                    control.Size = size;
                    using var form = new System.Windows.Forms.Form { Size = size, StartPosition = System.Windows.Forms.FormStartPosition.Manual, Location = new System.Drawing.Point(-2000, -2000) };
                    form.Controls.Add(control);
                    form.Show();
                    control.CreateControl();
                    form.PerformLayout();

                    using var bmp = new System.Drawing.Bitmap(size.Width, size.Height);
                    form.DrawToBitmap(bmp, new System.Drawing.Rectangle(System.Drawing.Point.Empty, size));

                    if (!string.IsNullOrEmpty(outDir) && System.IO.Directory.Exists(outDir))
                    {
                        var path = System.IO.Path.Combine(outDir, $"audit_analytics_{size.Width}x{size.Height}.png");
                        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    }

                    var refreshBtn = (DevExpress.XtraEditors.SimpleButton)typeof(CashierAuditAnalyticsControl)
                        .GetField("_btnRefresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(control)!;
                    var forecastBtn = (DevExpress.XtraEditors.SimpleButton)typeof(CashierAuditAnalyticsControl)
                        .GetField("_btnRunForecast", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(control)!;
                    var scanBtn = (DevExpress.XtraEditors.SimpleButton)typeof(CashierAuditAnalyticsControl)
                        .GetField("_btnRunScan", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(control)!;

                    Assert.True(refreshBtn.Height >= 28);
                    Assert.True(forecastBtn.Height >= 28);
                    Assert.True(scanBtn.Height >= 28);

                    var fRight = forecastBtn.PointToScreen(new System.Drawing.Point(forecastBtn.Width, 0)).X;
                    var frmRight = form.PointToScreen(new System.Drawing.Point(form.ClientSize.Width, 0)).X;
                    Assert.True(fRight <= frmRight);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)));
        if (error != null) throw error;
    }

    [Fact]
    public void RepeatedScaling_DoesNotCompound_OrRecurse()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IMediator>(new FakeMediator());

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var session = new FakeCurrentSession();

        using var control = new CashierAuditAnalyticsControl(scopeFactory, session);
        var form = new System.Windows.Forms.Form { Size = new System.Drawing.Size(1024, 768) };
        form.Controls.Add(control);
        form.Show();

        var scaleMethod = typeof(CashierAuditAnalyticsControl)
            .GetMethod("ScaleLayoutAtRuntime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        var refreshBtn = (DevExpress.XtraEditors.SimpleButton)typeof(CashierAuditAnalyticsControl)
            .GetField("_btnRefresh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(control)!;
        var forecastBtn = (DevExpress.XtraEditors.SimpleButton)typeof(CashierAuditAnalyticsControl)
            .GetField("_btnRunForecast", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(control)!;

        // Capture baseline sizes after first layout pass
        var initialRefreshSize = refreshBtn.Size;
        var initialForecastSize = forecastBtn.Size;

        // Simulate 50 consecutive resize / layout / DPI scaling cycles
        for (int i = 0; i < 50; i++)
        {
            scaleMethod.Invoke(control, null);
        }

        // Verify dimensions did NOT compound or drift
        Assert.Equal(initialRefreshSize, refreshBtn.Size);
        Assert.Equal(initialForecastSize, forecastBtn.Size);
    }

    private sealed class FakeMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) =>
            Task.FromResult(default(TResponse)!);

        public Task Send<TRequest>(TRequest request, System.Threading.CancellationToken cancellationToken = default) where TRequest : IRequest =>
            Task.CompletedTask;

        public Task<object?> Send(object request, System.Threading.CancellationToken cancellationToken = default) =>
            Task.FromResult<object?>(null);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, System.Threading.CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task Publish(object notification, System.Threading.CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, System.Threading.CancellationToken cancellationToken = default) where TNotification : INotification =>
            Task.CompletedTask;
    }

    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId => Guid.NewGuid();
        public Guid? SessionId => Guid.NewGuid();
        public string? UserName => "admin";
        public string? DisplayName => "Administrator";
        public string? UserDisplayName => "Administrator";
        public Guid? CompanyId => Guid.NewGuid();
        public Guid? BranchId => Guid.NewGuid();
        public Guid? TerminalId => Guid.NewGuid();
        public Guid? WarehouseId => Guid.NewGuid();
        public string? TerminalCode => "POS01";
        public bool IsAuthenticated => true;
        public IReadOnlySet<string> Permissions => new HashSet<string> { "menu.audit.analytics" };
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
        public event EventHandler? Changed { add { } remove { } }
    }
}
