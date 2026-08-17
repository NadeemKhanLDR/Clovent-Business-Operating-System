using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Clovent.Desktop.MasterData;

namespace DesignerTest;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            Console.WriteLine("Creating DesignSurface...");
            using var surface = new DesignSurface();
            
            Console.WriteLine("Loading TestDerivedForm into DesignSurface...");
            surface.BeginLoad(typeof(TestDerivedForm));

            if (surface.LoadErrors.Count > 0)
            {
                Console.WriteLine("--- DESIGNER LOAD ERRORS ---");
                foreach (Exception err in surface.LoadErrors)
                {
                    Console.WriteLine(err.ToString());
                    Console.WriteLine("----------------------------");
                }
            }
            else
            {
                Console.WriteLine("Successfully loaded in Designer!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("--- FATAL EXCEPTION ---");
            Console.WriteLine(ex.ToString());
        }
    }
}