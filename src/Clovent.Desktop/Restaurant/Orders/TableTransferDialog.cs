using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Prompt for moving a dine-in order to a different table.</summary>
public sealed class TableTransferDialog : MasterDataEditFormBase
{
    private readonly ComboBoxEdit _tableCombo = new();
    private readonly Dictionary<string, Guid?> _tablesByDisplay;

    /// <summary>Builds the dialog. <paramref name="tableOptions"/> should already exclude the order's current table.</summary>
    public TableTransferDialog(IReadOnlyList<(Guid Id, string Display)> tableOptions) : base("Transfer Table")
    {
        _tablesByDisplay = ComboBoxBinder.Bind(_tableCombo, tableOptions, includeEmpty: false);

        AddField("New Table:", _tableCombo);
    }

    /// <summary>The selected destination table.</summary>
    public Guid? NewTableId => ComboBoxBinder.GetSelectedId(_tableCombo, _tablesByDisplay);

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (NewTableId is null)
        {
            error = "Select a table.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
