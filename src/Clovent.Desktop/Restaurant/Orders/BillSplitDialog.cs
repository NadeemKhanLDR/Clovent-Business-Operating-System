using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Prompt for splitting a subset of an order's lines off to a new order at a
/// different table - the Bill Split Dialog. There is no dedicated "split
/// bill" domain command; picking which lines move plus a destination table
/// is exactly <c>SplitOrderCommand</c>'s shape (see its doc comment). Control
/// tree lives in <c>BillSplitDialog.Designer.cs</c>; this file holds
/// behavior only.
/// </summary>
public sealed partial class BillSplitDialog : MasterDataEditFormBase
{
    private readonly Dictionary<string, Guid?> _tablesByDisplay;

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public BillSplitDialog() : base("Split Bill")
    {
        _tablesByDisplay = null!;

        InitializeComponent();
        }

    /// <summary>Builds the dialog. <paramref name="lineOptions"/> should list only active (non-voided) lines.</summary>
    public BillSplitDialog(IReadOnlyList<(Guid Id, string Display)> lineOptions, IReadOnlyList<(Guid Id, string Display)> tableOptions) : base("Split Bill")
    {
        InitializeComponent();
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _tablesByDisplay = null!;
            return;
        }

        foreach (var (id, display) in lineOptions)
        {
            _linesList.Items.Add(new LineOption(id, display), CheckState.Unchecked);
        }

        _tablesByDisplay = ComboBoxBinder.Bind(_targetTableCombo, tableOptions, includeEmpty: false);
    }

    /// <summary>The order line ids the user checked.</summary>
    public IReadOnlyList<Guid> SelectedOrderLineIds =>
        [.. _linesList.CheckedItems.Cast<DevExpress.XtraEditors.Controls.CheckedListBoxItem>().Select(item => ((LineOption)item.Value).Id)];

    private sealed class LineOption(Guid id, string display)
    {
        public Guid Id { get; } = id;

        public override string ToString() => display;
    }

    /// <summary>The selected destination table.</summary>
    public Guid? TargetTableId => ComboBoxBinder.GetSelectedId(_targetTableCombo, _tablesByDisplay);

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (SelectedOrderLineIds.Count == 0)
        {
            error = "Select at least one line to move.";
            return false;
        }

        if (TargetTableId is null)
        {
            error = "Select a target table.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
