using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.UnitsOfMeasure;

/// <summary>
/// Create/edit dialog for a Unit of Measure - code and name. The code is
/// immutable after creation (mirrors <c>WarehouseEditForm</c>'s identical
/// "code fixed at creation" reasoning), so the field is disabled when
/// editing an existing unit. Control tree (fields, <c>AddField</c> calls)
/// lives in <c>UnitOfMeasureEditForm.Designer.cs</c>; this file holds
/// behavior only.
/// </summary>
public sealed partial class UnitOfMeasureEditForm : MasterDataEditFormBase
{
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public UnitOfMeasureEditForm() : base("Edit UOM")
    {
        InitializeComponent();
        }

    /// <summary>
    /// Builds the dialog. <paramref name="title"/> is the dialog's caption.
    /// Pass <paramref name="code"/> when editing so the (disabled) field
    /// still shows it - the code field is only enabled when
    /// <paramref name="isNew"/> is <see langword="true"/>.
    /// <paramref name="name"/> pre-populates the display name.
    /// </summary>
    public UnitOfMeasureEditForm(string title, string? code = null, string? name = null, bool isNew = true) : base(title)
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        _codeEdit.Text = code ?? string.Empty;
        _codeEdit.Enabled = isNew;
        _nameEdit.Text = name ?? string.Empty;
    }

    /// <summary>The entered code (only meaningful when creating).</summary>
    public string CodeValue => _codeEdit.Text.Trim();

    /// <summary>The entered display name.</summary>
    public string NameValue => _nameEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (_codeEdit.Enabled && string.IsNullOrWhiteSpace(_codeEdit.Text))
        {
            error = "Code is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_nameEdit.Text))
        {
            error = "Name is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
