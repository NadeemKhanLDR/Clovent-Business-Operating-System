using Clovent.Desktop.Startup;
using DevExpress.XtraEditors;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Runs one user-triggered action (a button click's whole handler),
/// catching anything it throws so a Restaurant screen can never crash out to
/// the global "Unhandled exception" dialog for a recoverable failure (a
/// concurrency conflict, a transient DB error, ...). The real exception is
/// logged for diagnostics; the user sees one plain sentence naming what
/// failed and why (never a stack trace or an internal identifier - see
/// <see cref="FriendlyErrorText"/>). The one shared implementation every
/// Restaurant screen's own <c>TryRunAsync</c>-shaped wrapper (<c>RestaurantPosView</c>,
/// <c>PaymentPanel</c>, <c>MenuItemsForm</c>) delegates to, rather than each
/// repeating the same try/catch/log/message-box shape.
/// </summary>
public static class GuardedAction
{
    /// <summary>
    /// Runs <paramref name="action"/>, showing a friendly message and
    /// logging the real exception if it throws. <paramref name="onFailureResync"/>
    /// (optional) re-syncs the screen against the server's actual current
    /// state afterward - e.g. re-running the same refresh a successful
    /// action would have triggered, so a failed action never leaves stale
    /// data on screen either.
    /// </summary>
    public static async Task RunAsync(IWin32Window owner, ILogger logger, Func<Task> action, string actionDescription, Func<Task>? onFailureResync = null)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            logger.LogError(ex, "Action failed: {Action}", actionDescription);

            XtraMessageBox.Show(
                owner,
                $"Unable to {actionDescription}.\n\nReason:\n{FriendlyErrorText.Summarize(ex)}\n\nPlease try again.",
                "Action Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            if (onFailureResync is null)
            {
                return;
            }

            try
            {
                await onFailureResync();
            }
            catch (Exception resyncEx) when (resyncEx is not OutOfMemoryException)
            {
                // The action's own failure already surfaced above; a failed
                // re-sync attempt on top of that is logged, not shown as a
                // second dialog stacked on the first.
                logger.LogError(resyncEx, "Failed to refresh after a failed action: {Action}", actionDescription);
            }
        }
    }
}
