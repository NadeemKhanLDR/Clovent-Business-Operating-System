using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;

namespace Clovent.Desktop.Identity.Users;

/// <summary>
/// Single new-password prompt shared by admin Reset Password (no current
/// password field) and self-service Change Password (adds one) - the two
/// differ only in whether the constructor's <c>requireCurrentPassword</c>
/// flag shows the extra field, matching how
/// <c>ResetPasswordCommand</c>/<c>ChangePasswordCommand</c> differ only in
/// whether the handler verifies a current password.
/// </summary>
public sealed class PasswordPromptForm : MasterDataEditFormBase
{
    private readonly TextEdit _currentPasswordEdit = new() { Properties = { PasswordChar = '*' } };
    private readonly TextEdit _newPasswordEdit = new() { Properties = { PasswordChar = '*' } };
    private readonly TextEdit _confirmPasswordEdit = new() { Properties = { PasswordChar = '*' } };

    /// <summary>Builds the dialog.</summary>
    public PasswordPromptForm(string title, bool requireCurrentPassword) : base(title)
    {
        if (requireCurrentPassword)
        {
            AddField("Current Password:", _currentPasswordEdit);
        }

        AddField("New Password:", _newPasswordEdit);
        AddField("Confirm Password:", _confirmPasswordEdit);
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
