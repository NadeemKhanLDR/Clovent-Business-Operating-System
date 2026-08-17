using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors.Controls;

namespace Clovent.Desktop.Forms.Identity.Roles;

/// <summary>
/// Create/edit dialog for a Role: name plus a permission checklist. This is
/// the Permission Assignment surface the brief calls for - permissions are
/// only ever assigned at the role level in this domain model (there is no
/// user-level permission concept), so a role's own editor is where
/// assignment happens, not a separate screen. Control tree (fields,
/// <c>AddField</c> calls, dialog sizing) lives in
/// <c>RoleEditForm.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class RoleEditForm : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid> _permissionsByDisplay = [];

    /// <summary>Builds the dialog.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RoleEditForm() : base("Edit Role")
    {
        InitializeComponent();
        }

    /// <summary>Builds the dialog. <paramref name="title"/> is the dialog's caption; <paramref name="name"/> pre-populates the name field when editing an existing role.</summary>
    public RoleEditForm(string title, string? name = null) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _nameEdit.Text = name ?? string.Empty;
    }

    /// <summary>The entered role name.</summary>
    public string RoleNameValue => _nameEdit.Text.Trim();

    /// <summary>
    /// The set of permission ids currently checked. Enumerates
    /// <c>CheckedListBoxItem.Value</c> rather than the items themselves -
    /// <c>CheckedItems</c> yields <see cref="CheckedListBoxItem"/> wrapper
    /// objects, not the raw display strings passed to <c>Items.Add</c>, so
    /// <c>.OfType&lt;string&gt;()</c> silently matched nothing and no
    /// permission was ever actually saved - confirmed via manual
    /// verification (a role's <c>PermissionIds</c> stayed <c>[]</c> in the
    /// database no matter what was checked here).
    /// </summary>
    public IReadOnlyCollection<Guid> SelectedPermissionIds =>
        [.. _permissionsList.CheckedItems.OfType<CheckedListBoxItem>()
            .Select(item => item.Value as string)
            .Where(display => display is not null && _permissionsByDisplay.ContainsKey(display))
            .Select(display => _permissionsByDisplay[display!])];

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
