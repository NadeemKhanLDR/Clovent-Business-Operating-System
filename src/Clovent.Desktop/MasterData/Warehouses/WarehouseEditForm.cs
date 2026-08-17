using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.MasterData.Warehouses;

/// <summary>
/// Create/edit dialog for a Warehouse - name and, for new warehouses only,
/// a code (immutable after creation, so the code field is disabled when
/// editing an existing warehouse).
/// </summary>
public sealed partial class WarehouseEditForm : MasterDataEditFormBase
{
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public WarehouseEditForm() : base("Edit Warehouse")
    {
        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog. <paramref name="title"/> is the dialog's caption;
    /// <paramref name="name"/> pre-populates the display name. Pass
    /// <paramref name="code"/> when editing so the (disabled) field still
    /// shows it - the field is only enabled when <paramref name="isNew"/> is
    /// <see langword="true"/>, since the code is immutable after creation.
    /// </summary>
    public WarehouseEditForm(string title, string? name = null, string? code = null, bool isNew = true) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _nameEdit.Text = name ?? string.Empty;
        _codeEdit.Text = code ?? string.Empty;
        _codeEdit.Enabled = isNew;
    }



    /// <summary>The entered warehouse name.</summary>
    public string WarehouseNameValue => _nameEdit.Text.Trim();

    /// <summary>The entered warehouse code (only meaningful when creating).</summary>
    public string CodeValue => _codeEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        if (_codeEdit.Enabled && string.IsNullOrWhiteSpace(_codeEdit.Text))
        {
            error = "Code is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
