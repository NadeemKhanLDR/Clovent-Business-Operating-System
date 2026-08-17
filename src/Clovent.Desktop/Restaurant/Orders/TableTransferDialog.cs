using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Prompt for moving a dine-in order to a different table. Control tree lives in <c>TableTransferDialog.Designer.cs</c>; this file holds behavior only.</summary>
public sealed partial class TableTransferDialog : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid?> _tablesByDisplay;

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public TableTransferDialog() : base("Transfer Table")
    {
        _tablesByDisplay = null!;

        InitializeComponent();
    }

    /// <summary>Builds the dialog. <paramref name="tableOptions"/> should already exclude the order's current table.</summary>
    public TableTransferDialog(IReadOnlyList<(Guid Id, string Display)> tableOptions) : base("Transfer Table")
    {
        InitializeComponent();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _tablesByDisplay = null!;
            return;
        }

        _tablesByDisplay = ComboBoxBinder.Bind(_tableCombo, tableOptions, includeEmpty: false);
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
