using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Identity.Users;

/// <summary>
/// Create/edit dialog for a User. Email and Username are only editable when
/// creating - <c>User</c>'s domain surface has no rename-email/rename-username
/// mutator (only <c>ChangeDisplayName</c>), so an existing user's identity
/// fields are shown read-only rather than silently discarding an edit the
/// domain can't apply. Company/Branch/Role assignment is offered here too,
/// even though each is its own command server-side - the caller
/// (<see cref="UserListView"/>) sends whichever of
/// <c>AssignUserCompanyCommand</c>/<c>AssignUserBranchCommand</c>/
/// <c>AssignUserToRoleCommand</c>/<c>RemoveUserFromRoleCommand</c> changed
/// after this dialog closes, one edit surface instead of four.
/// </summary>
public sealed class UserEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _emailEdit = new();
    private readonly TextEdit _userNameEdit = new();
    private readonly TextEdit _displayNameEdit = new();
    private readonly ComboBoxEdit _companyCombo = new();
    private readonly ComboBoxEdit _branchCombo = new();
    private readonly CheckedListBoxControl _rolesList = new() { Height = 90 };

    private readonly Dictionary<string, Guid> _companiesByDisplay = [];
    private readonly Dictionary<string, Guid> _branchesByDisplay = [];
    private readonly Dictionary<string, Guid> _rolesByDisplay = [];

    /// <summary>Builds the dialog. Pass <paramref name="isNew"/> <see langword="false"/> to lock Email/Username for an existing user.</summary>
    public UserEditForm(string title, bool isNew, string? email = null, string? userName = null, string? displayName = null)
        : base(title)
    {
        Height = 480;

        _emailEdit.Text = email ?? string.Empty;
        _emailEdit.Enabled = isNew;
        _userNameEdit.Text = userName ?? string.Empty;
        _userNameEdit.Enabled = isNew;
        _displayNameEdit.Text = displayName ?? string.Empty;

        _companyCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _branchCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _rolesList.CheckOnClick = true;

        AddField("Email:", _emailEdit);
        AddField("Username:", _userNameEdit);
        AddField("Display Name:", _displayNameEdit);
        AddField("Company:", _companyCombo);
        AddField("Branch:", _branchCombo);
        AddField("Roles:", _rolesList);
    }

    /// <summary>The entered email address.</summary>
    public string EmailValue => _emailEdit.Text.Trim();

    /// <summary>The entered login handle.</summary>
    public string UserNameValue => _userNameEdit.Text.Trim();

    /// <summary>The entered display name.</summary>
    public string DisplayNameValue => _displayNameEdit.Text.Trim();

    /// <summary>The selected company id, or <see langword="null"/> if none selected.</summary>
    public Guid? SelectedCompanyId => _companyCombo.SelectedItem is string display && _companiesByDisplay.TryGetValue(display, out var id) ? id : null;

    /// <summary>The selected branch id, or <see langword="null"/> if none selected.</summary>
    public Guid? SelectedBranchId => _branchCombo.SelectedItem is string display && _branchesByDisplay.TryGetValue(display, out var id) ? id : null;

    /// <summary>The set of role ids currently checked.</summary>
    public IReadOnlyCollection<Guid> SelectedRoleIds =>
        [.. _rolesList.CheckedItems.OfType<string>().Where(_rolesByDisplay.ContainsKey).Select(display => _rolesByDisplay[display])];

    /// <summary>Populates the Company combo. Call before showing the dialog.</summary>
    public void LoadCompanies(IReadOnlyList<(Guid Id, string Name)> companies, Guid? selected)
    {
        _companiesByDisplay.Clear();
        _companyCombo.Properties.Items.Clear();
        _companyCombo.Properties.Items.Add(string.Empty);
        foreach (var (id, name) in companies)
        {
            _companiesByDisplay[name] = id;
            _companyCombo.Properties.Items.Add(name);
        }

        _companyCombo.SelectedItem = selected is { } id2 && companies.FirstOrDefault(c => c.Id == id2) is { Name: var name2 } && name2 is not null
            ? name2
            : string.Empty;
    }

    /// <summary>Populates the Branch combo. Call before showing the dialog.</summary>
    public void LoadBranches(IReadOnlyList<(Guid Id, string Name)> branches, Guid? selected)
    {
        _branchesByDisplay.Clear();
        _branchCombo.Properties.Items.Clear();
        _branchCombo.Properties.Items.Add(string.Empty);
        foreach (var (id, name) in branches)
        {
            _branchesByDisplay[name] = id;
            _branchCombo.Properties.Items.Add(name);
        }

        _branchCombo.SelectedItem = selected is { } id2 && branches.FirstOrDefault(b => b.Id == id2) is { Name: var name2 } && name2 is not null
            ? name2
            : string.Empty;
    }

    /// <summary>Populates the Roles checklist. Call before showing the dialog.</summary>
    public void LoadRoles(IReadOnlyList<(Guid Id, string Name)> roles, IReadOnlyCollection<Guid> assigned)
    {
        _rolesByDisplay.Clear();
        _rolesList.Items.Clear();
        foreach (var (id, name) in roles)
        {
            _rolesByDisplay[name] = id;
            _rolesList.Items.Add(name, assigned.Contains(id));
        }
    }

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_emailEdit.Text))
        {
            error = "Email is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_userNameEdit.Text))
        {
            error = "Username is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_displayNameEdit.Text))
        {
            error = "Display Name is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
