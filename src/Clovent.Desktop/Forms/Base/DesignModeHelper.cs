using System.ComponentModel;
using System.Diagnostics;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Helper to determine if the code is executing inside a design tool (like Visual Studio WinForms Designer)
/// or at runtime.
/// </summary>
public static class DesignModeHelper
{
    /// <summary>
    /// Gets a value indicating whether the application is running in design mode.
    /// </summary>
    public static bool IsInDesignMode
    {
        get
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return true;

            var processName = Process.GetCurrentProcess().ProcessName.ToLowerInvariant();
            return processName.Contains("devenv") 
                   || processName.Contains("designtools") 
                   || processName.Contains("designhost");
        }
    }
}
