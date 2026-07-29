using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Prompt for merging one table's order into another's (two tables pushed together for one party).</summary>
public sealed class MergeTablesDialog : MasterDataEditFormBase
{
    private readonly ComboBoxEdit _sourceCombo = new();
    private readonly ComboBoxEdit _targetCombo = new();
    private readonly Dictionary<string, Guid?> _tablesByDisplay;

    /// <summary>Builds the dialog, preselecting <paramref name="defaultSourceTableId"/> as the source when given.</summary>
    public MergeTablesDialog(IReadOnlyList<(Guid Id, string Display)> tableOptions, Guid? defaultSourceTableId = null) : base("Merge Tables")
    {
        _tablesByDisplay = ComboBoxBinder.Bind(_sourceCombo, tableOptions, includeEmpty: false);
        ComboBoxBinder.Bind(_targetCombo, tableOptions, includeEmpty: false);
        ComboBoxBinder.SelectById(_sourceCombo, _tablesByDisplay, defaultSourceTableId);

        AddField("Source Table:", _sourceCombo);
        AddField("Target Table:", _targetCombo);
    }

    /// <summary>The table whose order will be merged away.</summary>
    public Guid? SourceTableId => ComboBoxBinder.GetSelectedId(_sourceCombo, _tablesByDisplay);

    /// <summary>The table the order will be merged into.</summary>
    public Guid? TargetTableId => ComboBoxBinder.GetSelectedId(_targetCombo, _tablesByDisplay);

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (SourceTableId is null || TargetTableId is null)
        {
            error = "Select both a source and target table.";
            return false;
        }

        if (SourceTableId == TargetTableId)
        {
            error = "Source and target tables must differ.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
