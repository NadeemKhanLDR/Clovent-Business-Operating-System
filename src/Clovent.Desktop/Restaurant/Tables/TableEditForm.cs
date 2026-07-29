using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Tables;

/// <summary>
/// Create/edit dialog for a Table - code (immutable after creation) and
/// seating capacity.
/// </summary>
public sealed class TableEditForm : MasterDataEditFormBase
{
    private readonly TextEdit _codeEdit = new();
    private readonly SpinEdit _capacityEdit = new() { Properties = { MinValue = 1, MaxValue = 100 } };

    /// <summary>Builds the dialog. Pass <paramref name="code"/> when editing so the (disabled) field still shows it.</summary>
    public TableEditForm(string title, string? code = null, int capacity = 2, bool isNew = true) : base(title)
    {
        _codeEdit.Text = code ?? string.Empty;
        _codeEdit.Enabled = isNew;
        _capacityEdit.Value = capacity;

        AddField("Code:", _codeEdit);
        AddField("Capacity:", _capacityEdit);
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
