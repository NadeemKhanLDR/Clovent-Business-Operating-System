using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Prompt for merging one table's order into another's (two tables pushed
/// together for one party). Control tree lives in
/// <c>MergeTablesDialog.Designer.cs</c>; this file holds behavior only.
/// </summary>
public sealed partial class MergeTablesDialog : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid?> _tablesByDisplay;

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public MergeTablesDialog() : base("Merge Tables")
    {
        _tablesByDisplay = null!;

        InitializeComponent();
        }

    /// <summary>Builds the dialog, preselecting <paramref name="defaultSourceTableId"/> as the source when given.</summary>
    public MergeTablesDialog(IReadOnlyList<(Guid Id, string Display)> tableOptions, Guid? defaultSourceTableId = null) : base("Merge Tables")
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _tablesByDisplay = null!;
            return;
        }

        _tablesByDisplay = ComboBoxBinder.Bind(_sourceCombo, tableOptions, includeEmpty: false);
        ComboBoxBinder.Bind(_targetCombo, tableOptions, includeEmpty: false);
        ComboBoxBinder.SelectById(_sourceCombo, _tablesByDisplay, defaultSourceTableId);
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
