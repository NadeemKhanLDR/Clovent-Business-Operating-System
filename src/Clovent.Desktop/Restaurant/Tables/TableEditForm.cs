using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Restaurant.Tables;

/// <summary>
/// Create/edit dialog for a Table - code (immutable after creation) and
/// seating capacity. Control tree lives in <c>TableEditForm.Designer.cs</c>;
/// this file holds behavior only.
/// </summary>
public sealed partial class TableEditForm : MasterDataEditFormBase
{
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public TableEditForm() : base("Edit Table")
    {
        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog. <paramref name="title"/> is the dialog's caption.
    /// Pass <paramref name="code"/> when editing so the (disabled) field
    /// still shows it - the field is only enabled when
    /// <paramref name="isNew"/> is <see langword="true"/>, since the code is
    /// immutable after creation. <paramref name="capacity"/> pre-populates
    /// the seating capacity.
    /// </summary>
    public TableEditForm(string title, string? code = null, int capacity = 2, bool isNew = true) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _codeEdit.Text = code ?? string.Empty;
        _codeEdit.Enabled = isNew;
        _capacityEdit.Value = capacity;
    }



    /// <summary>The entered table code (only meaningful when creating).</summary>
    public string CodeValue => _codeEdit.Text.Trim();

    /// <summary>The entered seating capacity.</summary>
    public int CapacityValue => (int)_capacityEdit.Value;

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (_codeEdit.Enabled && string.IsNullOrWhiteSpace(_codeEdit.Text))
        {
            error = "Code is required.";
            return false;
        }

        if (CapacityValue < 1)
        {
            error = "Capacity must be at least 1.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
