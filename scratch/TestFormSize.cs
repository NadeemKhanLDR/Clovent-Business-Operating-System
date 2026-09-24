using System;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.Restaurant.SmartPos;
using DevExpress.XtraEditors;

public class TestFormSize
{
    [STAThread]
    public static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Console.WriteLine($"Application.HighDpiMode: {Application.HighDpiMode}");

        var form = new QuickOrderTemplateEditForm("Edit Quick Order Template", Array.Empty<ProductOptionRow>());
        Console.WriteLine($"After Constructor: Size={form.Size}, ClientSize={form.ClientSize}, MinSize={form.MinimumSize}, DeviceDpi={form.DeviceDpi}");

        form.Shown += (s, e) =>
        {
            Console.WriteLine($"After Shown: Size={form.Size}, ClientSize={form.ClientSize}, MinSize={form.MinimumSize}, Bounds={form.Bounds}, DeviceDpi={form.DeviceDpi}");
            Application.Exit();
        };

        Application.Run(form);
    }
}
