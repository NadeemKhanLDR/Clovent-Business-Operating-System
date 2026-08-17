using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Restaurant.DiningAreas;

/// <summary>Create/edit dialog for a Dining Area - name only. Control tree lives in <c>DiningAreaEditForm.Designer.cs</c>; this file holds behavior only.</summary>
public sealed partial class DiningAreaEditForm : MasterDataEditFormBase
{
    /// <summary>Builds the dialog.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public DiningAreaEditForm() : base("Edit Dining Area")
    {
        InitializeComponent();
        }

    /// <summary>Builds the dialog. <paramref name="title"/> is the dialog's caption; <paramref name="name"/> pre-populates the name field when editing an existing dining area.</summary>
    public DiningAreaEditForm(string title, string? name = null) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _nameEdit.Text = name ?? string.Empty;
    }

    /// <summary>The entered dining area name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

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
