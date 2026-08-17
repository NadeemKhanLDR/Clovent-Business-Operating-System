namespace Clovent.Desktop.Forms.Base;

/// <summary>
/// Persists and restores a resizable dialog's last size/position/maximized
/// state, keyed by a caller-supplied name - the one shared mechanism every
/// resizable Restaurant popup (<c>ReceiptPreviewForm</c>, every
/// <c>MasterDataEditFormBase</c>-derived edit dialog, ...) uses, rather than
/// each dialog inventing its own persistence. Stored as a plain local file
/// under the current
/// user's local application data folder - the same file-convention-not-a-
/// database-column choice <c>MenuItemImageStore</c>/<c>LanguagePreferenceStore</c>
/// already establish for a Desktop-local, single-user, single-machine
/// preference nothing outside this process needs to know about.
/// </summary>
internal static class WindowPlacementStore
{
    private static readonly string RootDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "WindowPlacements");

    /// <summary>
    /// Restores <paramref name="form"/>'s last saved size/position, if one
    /// was saved and its top-left corner still lands on a currently
    /// connected screen (a saved position from a monitor configuration that
    /// no longer exists would otherwise place the window off-screen and
    /// unreachable) - falls back to whatever <see cref="Form.StartPosition"/>/
    /// <see cref="Control.Size"/> the caller already set otherwise. Call
    /// after <see cref="Control.MinimumSize"/> is set, so a saved size below
    /// the current minimum is clamped up rather than silently violated.
    /// </summary>
    public static void Restore(Form form, string key)
    {
        var path = GetPath(key);
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            var parts = File.ReadAllText(path).Split(',');
            if (parts.Length != 5)
            {
                return;
            }

            var bounds = new Rectangle(
                int.Parse(parts[0]), int.Parse(parts[1]),
                int.Parse(parts[2]), int.Parse(parts[3]));
            var maximized = parts[4] == "1";

            if (Screen.AllScreens.Any(s => s.WorkingArea.Contains(bounds.Location)))
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = bounds.Location;
                form.Size = new Size(
                    Math.Max(bounds.Width, form.MinimumSize.Width),
                    Math.Max(bounds.Height, form.MinimumSize.Height));
            }

            if (maximized)
            {
                form.WindowState = FormWindowState.Maximized;
            }
        }
        catch (Exception ex) when (ex is IOException or FormatException or OverflowException)
        {
            // Corrupt/unreadable placement file - fall back to whatever
            // default the caller already set rather than failing to open
            // the dialog at all.
        }
    }

    /// <summary>Saves <paramref name="form"/>'s current size/position/maximized state for the next dialog opened with the same <paramref name="key"/>. Call from a <see cref="Form.FormClosed"/> handler.</summary>
    public static void Save(Form form, string key)
    {
        var bounds = form.WindowState == FormWindowState.Normal ? form.Bounds : form.RestoreBounds;
        var maximized = form.WindowState == FormWindowState.Maximized ? "1" : "0";

        try
        {
            Directory.CreateDirectory(RootDirectory);
            File.WriteAllText(GetPath(key), $"{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height},{maximized}");
        }
        catch (IOException)
        {
        }
    }

    private static string GetPath(string key) => Path.Combine(RootDirectory, $"{key}.txt");
}
