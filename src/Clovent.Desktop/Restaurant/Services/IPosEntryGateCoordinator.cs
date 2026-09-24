using System.Threading.Tasks;
using System.Windows.Forms;

namespace Clovent.Desktop.Restaurant.Services;

/// <summary>
/// Canonical coordinator for the POS Entry Shift-Access Gate.
/// Decides whether the authenticated cashier may transition into Restaurant POS mode,
/// prompts for Open Shift when needed, resumes an existing open shift, or displays blocking
/// diagnostics for terminal or cashier conflicts.
/// </summary>
public interface IPosEntryGateCoordinator
{
    /// <summary>
    /// Executes the canonical POS shift access gate.
    /// If an open shift is active for this cashier, resumes it and opens POS.
    /// If a shift is required, prompts via <see cref="Shifts.OpenShiftDialog"/>; if confirmed, opens POS.
    /// If another cashier owns the terminal shift or user has a shift elsewhere, blocks entry and displays a clear message.
    /// Returns true if POS was successfully opened; false otherwise.
    /// </summary>
    Task<bool> EnsureShiftAndOpenPosAsync(IWin32Window? owner = null);
}
