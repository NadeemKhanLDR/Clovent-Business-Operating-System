using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Restaurant.Shared;

/// <summary>
/// Collects a manager's credentials before a privileged action the signed-in
/// operator cannot perform alone - a credit-limit override or an order void.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately collects only; it verifies nothing. The caller passes what it
/// gathers to <see cref="Authorization.IManagerAuthorizationService"/>, so the
/// credential check stays in one place and this dialog holds no authentication
/// logic, no hashing, and no knowledge of which permission is being demanded.
/// </para>
/// <para>
/// The header states plainly that this is a manager authorization step, and
/// the detail text spells out exactly what is being authorized, so an
/// operator can never mistake it for an ordinary confirmation prompt - the
/// Yes/No box it replaces gave no indication that anything was being
/// authorized at all (defects D7/D25).
/// </para>
/// </remarks>
public sealed partial class ManagerAuthorizationForm : MasterDataEditFormBase
{
    /// <summary>Designer-only constructor.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public ManagerAuthorizationForm() : base("Manager Authorization")
    {
        InitializeComponent();
    }

    /// <summary>Builds the challenge dialog.</summary>
    /// <param name="title">The window title, naming the action (e.g. "Manager Authorization - Credit Limit Override").</param>
    /// <param name="detail">A full description of what approving will do, including the amounts involved.</param>
    public ManagerAuthorizationForm(string title, string detail) : base(title)
    {
        InitializeComponent();

        _detailLabel.Text = detail;

        _userNameEdit.Dock = System.Windows.Forms.DockStyle.Fill;
        _passwordEdit.Dock = System.Windows.Forms.DockStyle.Fill;

        this.ClientSize = new System.Drawing.Size(420, 160);
    }

    /// <summary>The submitted manager username, trimmed.</summary>
    public string ManagerUserName => _userNameEdit.Text.Trim();

    /// <summary>The submitted manager password, exactly as typed.</summary>
    public string ManagerPassword => _passwordEdit.Text;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(ManagerUserName))
        {
            error = "Enter the manager's username.";
            return false;
        }

        if (string.IsNullOrEmpty(ManagerPassword))
        {
            error = "Enter the manager's password.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
