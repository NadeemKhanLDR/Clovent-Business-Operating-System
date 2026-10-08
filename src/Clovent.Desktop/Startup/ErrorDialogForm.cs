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
    /// <param name="customMessage">Optional custom friendly message to display in the main label.</param>
    public ErrorDialogForm(Exception exception, string? context, string? customMessage = null) : base()
    {
        InitializeComponent();

        ArgumentNullException.ThrowIfNull(exception);

        _exception = exception;

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            _messageLabel.Text = customMessage;
        }
        else
        {
            var summary = context is null
                ? "An unexpected error occurred."
                : $"An unexpected error occurred ({context}).";

            _messageLabel.Text = $"{summary}\n\n{exception.Message}";
        }
        _detailsMemo.Text = exception.ToString();
    }

    /// <inheritdoc/>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        ApplyDpiScaling();
    }

    private void ApplyDpiScaling()
    {
        int btnHeight = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this);
        int padV = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(6, this);
        int padH = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(12, this);

        _buttonPanel.Height = btnHeight + (padV * 2);
        _buttonPanel.Padding = new Padding(padH, padV, padH, padV);

        _detailsToggle.Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(130, this);
        _copyButton.Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(130, this);
        _copyButton.Margin = new Padding(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(8, this), 0, 0, 0);
        _closeButton.Width = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(100, this);

        int minW = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(500, this);
        int minH = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(180, this);
        MinimumSize = new Size(minW, minH);

        int targetW = Clovent.Desktop.Forms.Base.DesktopDpi.Scale(580, this);
        int targetH = _detailsVisible
            ? Clovent.Desktop.Forms.Base.DesktopDpi.Scale(440, this)
            : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(220, this);

        _messageLabel.PerformLayout();
        int requiredMsgHeight = _messageLabel.Height + _buttonPanel.Height + Clovent.Desktop.Forms.Base.DesktopDpi.Scale(20, this);
        if (!_detailsVisible && targetH < requiredMsgHeight)
        {
            targetH = requiredMsgHeight;
        }

        ClientSize = new Size(Math.Max(ClientSize.Width, targetW), targetH);
    }

    private void DetailsToggle_Click(object? sender, EventArgs e) => ToggleDetails();

    private void CopyButton_Click(object? sender, EventArgs e) => Clipboard.SetText(_exception.ToString());

    private void CloseButton_Click(object? sender, EventArgs e) => Close();

    private void ToggleDetails()
    {
        _detailsVisible = !_detailsVisible;
        _detailsMemo.Visible = _detailsVisible;
        _detailsToggle.Text = _detailsVisible ? "Hide Details" : "Show Details";

        int targetH = _detailsVisible
            ? Clovent.Desktop.Forms.Base.DesktopDpi.Scale(440, this)
            : Clovent.Desktop.Forms.Base.DesktopDpi.Scale(220, this);

        if (!_detailsVisible)
        {
            _messageLabel.PerformLayout();
            int requiredMsgHeight = _messageLabel.Height + _buttonPanel.Height + Clovent.Desktop.Forms.Base.DesktopDpi.Scale(20, this);
            if (targetH < requiredMsgHeight)
            {
                targetH = requiredMsgHeight;
            }
        }

        ClientSize = new Size(ClientSize.Width, targetH);
    }
}
