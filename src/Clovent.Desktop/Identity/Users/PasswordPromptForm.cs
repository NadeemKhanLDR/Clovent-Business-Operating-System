using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Identity.Users;

/// <summary>
/// Single new-password prompt shared by admin Reset Password (no current
/// password field) and self-service Change Password (adds one) - the two
/// differ only in whether the constructor's <c>requireCurrentPassword</c>
/// flag shows the extra field, matching how
/// <c>ResetPasswordCommand</c>/<c>ChangePasswordCommand</c> differ only in
/// whether the handler verifies a current password. Control tree lives in
/// <c>PasswordPromptForm.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class PasswordPromptForm : MasterDataEditFormBase
{
    /// <summary>Builds the dialog.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public PasswordPromptForm() : base("Change Password")
    {
        InitializeComponent();
    }

    /// <summary>Builds the dialog. <paramref name="title"/> is the dialog's caption; when <paramref name="requireCurrentPassword"/> is <see langword="true"/> an extra Current Password field is shown, for self-service Change Password.</summary>
    public PasswordPromptForm(string title, bool requireCurrentPassword) : base(title)
    {
        InitializeComponent();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        if (!requireCurrentPassword) { _currentPasswordEdit.Visible = false; label1.Visible = false; }
    }

    /// <summary>The entered current password (empty if not requested).</summary>
    public string CurrentPassword => _currentPasswordEdit.Text;

    /// <summary>The entered new password.</summary>
    public string NewPassword => _newPasswordEdit.Text;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrEmpty(_newPasswordEdit.Text))
        {
            error = "New password is required.";
            return false;
        }

        if (_newPasswordEdit.Text != _confirmPasswordEdit.Text)
        {
            error = "New password and confirmation do not match.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
