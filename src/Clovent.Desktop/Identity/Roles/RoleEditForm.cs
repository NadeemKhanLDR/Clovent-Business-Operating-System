using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Identity.Roles;

/// <summary>
/// Create/edit dialog for a Role: name plus a permission checklist. This is
/// the Permission Assignment surface the brief calls for - permissions are
/// only ever assigned at the role level in this domain model (there is no
/// user-level permission concept), so a role's own editor is where
/// assignment happens, not a separate screen.
/// </summary>
public sealed class RoleEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _nameEdit = new();
    private readonly CheckedListBoxControl _permissionsList = new() { Height = 220 };

    private readonly Dictionary<string, Guid> _permissionsByDisplay = [];

    /// <summary>Builds the dialog.</summary>
    public RoleEditForm(string title, string? name = null) : base(title)
    {
        Height = 420;
        _nameEdit.Text = name ?? string.Empty;
        _permissionsList.CheckOnClick = true;

        AddField("Name:", _nameEdit);
        AddField("Permissions:", _permissionsList);
    }

    /// <summary>The entered role name.</summary>
    public string RoleNameValue => _nameEdit.Text.Trim();

    /// <summary>The set of permission ids currently checked.</summary>
    public IReadOnlyCollection<Guid> SelectedPermissionIds =>
        [.. _permissionsList.CheckedItems.OfType<string>().Where(_permissionsByDisplay.ContainsKey).Select(display => _permissionsByDisplay[display])];

    /// <summary>Populates the permission checklist. Call before showing the dialog.</summary>
    public void LoadPermissions(IReadOnlyList<(Guid Id, string Code, string Description)> permissions, IReadOnlyCollection<Guid> assigned)
    {
        _permissionsByDisplay.Clear();
        _permissionsList.Items.Clear();
        foreach (var (id, code, description) in permissions)
        {
            var display = $"{code} - {description}";
            _permissionsByDisplay[display] = id;
            _permissionsList.Items.Add(display, assigned.Contains(id));
        }
    }

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
