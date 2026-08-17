using DevExpress.XtraEditors;

namespace Clovent.Desktop.Startup;

/// <summary>
/// The global error dialog: a short message plus an expandable details
/// panel with the full exception text and a copy-to-clipboard button. Built
/// entirely in code - see <see cref="Clovent.Desktop.Forms.Shell.MainForm"/>'s doc comment for why.
/// </summary>
public sealed partial class ErrorDialogForm : XtraForm
{
    private readonly Exception _exception;
    private bool _detailsVisible;

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ErrorDialogForm()
    {
        _exception = null!;

        InitializeComponent();
    }

    /// <summary>Builds the dialog for the given exception.</summary>
    /// <param name="exception">The exception to display.</param>
    /// <param name="context">Optional short description of where the exception was caught.</param>
    public ErrorDialogForm(Exception exception, string? context) : base()
    {
        InitializeComponent();

        ArgumentNullException.ThrowIfNull(exception);

        _exception = exception;

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        var summary = context is null
            ? "An unexpected error occurred."
            : $"An unexpected error occurred ({context}).";

        _messageLabel.Text = $"{summary}\n\n{exception.Message}";
        _detailsMemo.Text = exception.ToString();
    }

    private void DetailsToggle_Click(object? sender, EventArgs e) => ToggleDetails();

    private void CopyButton_Click(object? sender, EventArgs e) => Clipboard.SetText(_exception.ToString());

    private void CloseButton_Click(object? sender, EventArgs e) => Close();

    private void ToggleDetails()
    {
        _detailsVisible = !_detailsVisible;
        _detailsMemo.Visible = _detailsVisible;
        _detailsToggle.Text = _detailsVisible ? "Hide Details" : "Show Details";
        Height = _detailsVisible ? 420 : 200;
    }
}
