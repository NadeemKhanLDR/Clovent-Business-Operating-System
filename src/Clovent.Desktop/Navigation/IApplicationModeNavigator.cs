using System.Windows.Forms;
using Clovent.Desktop.Forms.Shell;

namespace Clovent.Desktop.Navigation;

/// <summary>
/// Coordinates top-level application window mode transitions between Restaurant POS
/// and Back Office shell. Enforces the single-window invariant: at runtime exactly one
/// main application window is active and alive; the previous window is disposed cleanly.
/// </summary>
public interface IApplicationModeNavigator
{
    /// <summary>The currently active main top-level form.</summary>
    Form? CurrentForm { get; }

    /// <summary>The active workspace host when in Back Office mode; null otherwise.</summary>
    IWorkspaceHost? CurrentWorkspaceHost { get; }

    /// <summary>True while a mode transition is in progress, guarding against reentrant requests.</summary>
    bool IsTransitioning { get; }

    /// <summary>The application context managing the process message loop lifetime.</summary>
    ApplicationContext ApplicationContext { get; }

    /// <summary>
    /// Transitions to Restaurant POS mode: prepares a fresh <c>RestaurantPosForm</c>,
    /// applies the active shift, disposes the current Back Office window, and displays the POS window.
    /// </summary>
    Task OpenPosAsync(Clovent.Restaurant.Application.Shifts.Dtos.ShiftDto? activeShift = null);

    /// <summary>
    /// Transitions to Back Office mode: prepares a fresh <c>MainForm</c>,
    /// opens the initial document view, disposes the current POS window, and displays Back Office.
    /// </summary>
    Task OpenBackOfficeAsync(string? initialViewKey = "dashboard", string? initialCaption = "Dashboard");

    /// <summary>
    /// Transitions to Login mode: signs out the in-memory session, disposes the active main window,
    /// and displays a fresh <c>LoginForm</c> within the application context.
    /// Does NOT punch out the employee (attendance session remains open in database).
    /// </summary>
    Task OpenLoginAsync();

    /// <summary>Exits the application context and ends the process message loop.</summary>
    void ExitApplication();

    /// <summary>Exits the application context and ends the process message loop with initiator context.</summary>
    void ExitApplication(string initiator) => ExitApplication();

    /// <summary>Updates or sets the UI synchronization context used to marshal mode switches to the main thread.</summary>
    void SetUiSynchronizationContext(System.Threading.SynchronizationContext syncContext) { }
}
