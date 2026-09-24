using System;
using System.Windows.Forms;

namespace Clovent.Desktop.Navigation;

/// <summary>
/// Owns the top-level Windows Forms message loop lifetime for the CBOS application.
/// Distinguishes between intentional application mode transitions (which swap the
/// main form without exiting) and genuine window closes (which exit the message loop).
/// </summary>
public sealed class CbosApplicationContext : ApplicationContext
{
    private bool _isTransitioning;

    /// <summary>
    /// When true, window closes triggered by mode switches do not terminate the message loop.
    /// </summary>
    public bool IsTransitioning
    {
        get => _isTransitioning;
        set => _isTransitioning = value;
    }

    /// <summary>Sets the current active top-level form.</summary>
    public void SetActiveForm(Form form)
    {
        ArgumentNullException.ThrowIfNull(form);
        MainForm = form;
    }

    /// <summary>Clears the current active form reference.</summary>
    public void ClearActiveForm()
    {
        MainForm = null;
    }

    /// <summary>Explicitly terminates the application message loop.</summary>
    public void Exit()
    {
        ExitThreadCore();
    }

    /// <inheritdoc/>
    protected override void OnMainFormClosed(object? sender, EventArgs e)
    {
        System.Diagnostics.Trace.WriteLine($"[CbosAppContext] OnMainFormClosed: sender={sender?.GetType().Name}, activeMainForm={MainForm?.GetType().Name}, isTransitioning={_isTransitioning}, threadId={Environment.CurrentManagedThreadId}");

        if (_isTransitioning)
        {
            // Transitioning between modes: suppress thread exit while disposing previous form.
            return;
        }

        if (sender != null && MainForm != null && !ReferenceEquals(sender, MainForm))
        {
            // Previous form closed after transition completed: ignore and do not exit thread.
            return;
        }

        // Genuine close: exit the application message loop.
        base.OnMainFormClosed(sender, e);
    }

    /// <inheritdoc/>
    protected override void ExitThreadCore()
    {
        System.Diagnostics.Trace.WriteLine($"[CbosAppContext] ExitThreadCore called on Thread {Environment.CurrentManagedThreadId}");
        base.ExitThreadCore();
    }
}
