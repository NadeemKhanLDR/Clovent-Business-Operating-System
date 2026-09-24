using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Forms.Restaurant.MenuItems;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Startup;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Restaurant.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests;

public class CategorySelectionLiveScreenshotsTest
{
    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint nFlags);

    private static void CaptureForm(Form form, string outPath)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(0, 0);
        form.Show();
        for (int i = 0; i < 15; i++)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }
        var bounds = form.Bounds;
        using var bmp = new Bitmap(bounds.Width, bounds.Height);
        using (var g = Graphics.FromImage(bmp))
        {
            var hdc = g.GetHdc();
            try
            {
                PrintWindow(form.Handle, hdc, 2);
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }
        bmp.Save(outPath, ImageFormat.Png);
    }

    [Fact(Skip = "Interactive live desktop UI test; executed manually or via dedicated UI test harness.")]
    public void Generate_Live_Category_Selection_Screenshots()
    {
        var thread = new Thread(() =>
        {
            var host = LiveAcceptanceQaRun.Host.Value;
            var session = host.Services.GetRequiredService<ICurrentSession>();
            using (var scope = host.Services.CreateScope())
            {
                var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var admin = identityDb.Users.AsEnumerable().First(u => u.UserName.Value == "admin");
                session.SignIn(admin.Id.Value, Guid.NewGuid(), admin.UserName.Value);
            }

            var qaDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "qa", "category_selected_live"));
            Directory.CreateDirectory(qaDir);

            using var posForm = new RestaurantPosForm(
                host.Services.GetRequiredService<IServiceScopeFactory>(),
                session,
                host.Services.GetRequiredService<IMenuItemsChangeNotifier>(),
                host.Services.GetRequiredService<IManagerAuthorizationService>(),
                host.Services.GetRequiredService<ISplashScreenService>());

            posForm.Size = new Size(1366, 768);
            posForm.Show();
            Application.DoEvents();

            // Force load core menu items & controls synchronously
            var loadMethod = typeof(RestaurantPosForm).GetMethod("LoadCoreAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            if (loadMethod != null)
            {
                var task = (Task)loadMethod.Invoke(posForm, null)!;
                task.GetAwaiter().GetResult();
            }

            Application.DoEvents();
            Thread.Sleep(200);

            var panelField = typeof(RestaurantPosForm).GetField("_categoryButtonsPanel", BindingFlags.NonPublic | BindingFlags.Instance);
            var panel = panelField?.GetValue(posForm) as FlowLayoutPanel;
            Assert.NotNull(panel);

            var controls = panel.Controls.OfType<DevExpress.XtraEditors.PanelControl>().ToList();
            Assert.NotEmpty(controls);

            int index = 1;
            foreach (var card in controls)
            {
                // Trigger click on card
                card.Invoke(new Action(() =>
                {
                    typeof(Control).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?.Invoke(card, [EventArgs.Empty]);
                }));

                for (int j = 0; j < 10; j++)
                {
                    Application.DoEvents();
                    Thread.Sleep(20);
                }

                string filename = $"cat_{index:D2}.png";
                CaptureForm(posForm, Path.Combine(qaDir, filename));
                index++;
            }

            posForm.Close();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}
