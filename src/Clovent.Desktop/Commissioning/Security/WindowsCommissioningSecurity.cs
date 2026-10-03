using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace Clovent.Desktop.Commissioning.Security;

/// <summary>
/// Provides helper methods to verify Windows Administrator privileges, trigger UAC elevation restarts,
/// and ensure administrative privileges during commissioning operations.
/// </summary>
public static class WindowsCommissioningSecurity
{
    /// <summary>
    /// Checks whether the current process is running with elevated Windows Administrator privileges.
    /// </summary>
    /// <returns>True if the process has Windows Administrator privileges; otherwise, false.</returns>
    public static bool IsRunningAsAdministrator()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            if (identity == null)
            {
                return false;
            }

            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Restarts the current process with elevated Windows Administrator permissions using the "runas" verb.
    /// </summary>
    /// <param name="arguments">Optional command-line arguments to pass to the restarted process.</param>
    /// <returns>True if the elevated process was successfully started; otherwise, false.</returns>
    public static bool RestartAsAdministrator(string? arguments = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            processPath = Process.GetCurrentProcess().MainModule?.FileName;
        }

        if (string.IsNullOrEmpty(processPath))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = processPath,
            Arguments = arguments ?? string.Empty,
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            var proc = Process.Start(startInfo);
            return proc != null;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // The user canceled the UAC elevation prompt (ERROR_CANCELLED)
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that the process is elevated for a commissioning operation.
    /// If already elevated, returns true.
    /// If not elevated, prompts the user via a standard WinForms MessageBox whether to restart with UAC elevation;
    /// if confirmed, launches the elevated process and returns false.
    /// </summary>
    /// <param name="operationDescription">A description of the operation requiring elevation.</param>
    /// <returns>True if currently running as Administrator; otherwise, false.</returns>
    public static bool EnsureElevatedForCommissioning(string operationDescription)
    {
        if (IsRunningAsAdministrator())
        {
            return true;
        }

        var message = $"The operation '{operationDescription}' requires Windows Administrator elevation.\n\n" +
                      "Would you like to restart the application as Administrator?";

        var result = MessageBox.Show(
            message,
            "Administrator Elevation Required",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            if (RestartAsAdministrator())
            {
                Application.Exit();
            }
        }

        return false;
    }
}
