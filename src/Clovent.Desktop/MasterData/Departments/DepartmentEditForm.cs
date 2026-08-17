using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.MasterData.Departments;

/// <summary>Create/edit dialog for a Department - just a name.</summary>
public sealed partial class DepartmentEditForm : MasterDataEditFormBase
{
    /// <summary>Builds the dialog, pre-filled when editing an existing department.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public DepartmentEditForm() : base("Edit Department")
    {
        InitializeComponent();
        }

    /// <summary>Builds the dialog. <paramref name="title"/> is the dialog's caption; <paramref name="name"/> pre-populates the name field when editing an existing department.</summary>
    public DepartmentEditForm(string title, string? name = null) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _nameEdit.Text = name ?? string.Empty;
    }



    /// <summary>The entered department name.</summary>
    public string DepartmentNameValue => _nameEdit.Text.Trim();

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
