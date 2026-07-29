using DevExpress.XtraEditors;

namespace Clovent.Desktop.Startup;

/// <summary>
/// The global error dialog: a short message plus an expandable details
/// panel with the full exception text and a copy-to-clipboard button. Built
/// entirely in code - see <see cref="Shell.ShellForm"/>'s doc comment for why.
/// </summary>
public sealed class ErrorDialogForm : XtraForm
{
    private readonly MemoEdit _detailsMemo;
    private readonly SimpleButton _detailsToggle;
    private bool _detailsVisible;

    /// <summary>Builds the dialog for the given exception.</summary>
    /// <param name="exception">The exception to display.</param>
    /// <param name="context">Optional short description of where the exception was caught.</param>
    public ErrorDialogForm(Exception exception, string? context)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Text = "An error occurred";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 560;
        Height = 200;
        MinimumSize = new Size(420, 160);
        MaximizeBox = false;
        MinimizeBox = false;

        var summary = context is null
            ? "An unexpected error occurred."
            : $"An unexpected error occurred ({context}).";

        var messageLabel = new LabelControl
        {
            Text = $"{summary}\n\n{exception.Message}",
            Dock = DockStyle.Top,
            Padding = new Padding(16, 16, 16, 8),
            AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical,
        };

        _detailsMemo = new MemoEdit
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Visible = false,
            Text = exception.ToString(),
        };
        _detailsMemo.Properties.ScrollBars = ScrollBars.Vertical;

        var buttonPanel = new PanelControl
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
        };

        _detailsToggle = new SimpleButton { Text = "Show Details", Dock = DockStyle.Left, Width = 120 };
        _detailsToggle.Click += (_, _) => ToggleDetails();

        var copyButton = new SimpleButton { Text = "Copy Details", Dock = DockStyle.Left, Width = 120 };
        copyButton.Click += (_, _) => Clipboard.SetText(exception.ToString());

        var closeButton = new SimpleButton { Text = "Close", Dock = DockStyle.Right, Width = 100, DialogResult = DialogResult.OK };
        closeButton.Click += (_, _) => Close();

        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(copyButton);
        buttonPanel.Controls.Add(_detailsToggle);

        Controls.Add(_detailsMemo);
        Controls.Add(buttonPanel);
        Controls.Add(messageLabel);

        AcceptButton = closeButton;
        CancelButton = closeButton;
    }

    private void ToggleDetails()
    {
        _detailsVisible = !_detailsVisible;
        _detailsMemo.Visible = _detailsVisible;
        _detailsToggle.Text = _detailsVisible ? "Hide Details" : "Show Details";
        Height = _detailsVisible ? 420 : 200;
    }
}
