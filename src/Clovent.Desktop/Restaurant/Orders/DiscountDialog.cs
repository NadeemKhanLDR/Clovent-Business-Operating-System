using Clovent.Desktop.MasterData;
using Clovent.Restaurant.Discounts;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Prompt for applying a discount to an order - type, value, and reason.
/// Control tree lives in <c>DiscountDialog.Designer.cs</c>; this file holds
/// behavior only.
/// </summary>
public sealed partial class DiscountDialog : MasterDataEditFormBase
{
    /// <summary>Builds the dialog.</summary>
    public DiscountDialog() : base("Apply Discount")
    {
        InitializeComponent();
        }

    /// <summary>The selected discount type.</summary>
    public DiscountType DiscountType => Enum.Parse<DiscountType>((string)_typeCombo.SelectedItem);

    /// <summary>The entered value - a percentage (0-100) or a fixed amount, per <see cref="DiscountType"/>.</summary>
    public decimal Value => _valueEdit.Value;

    /// <summary>The entered reason.</summary>
    public string Reason => _reasonEdit.Text.Trim();

    /// <inheritdoc/>
    protected override bool ValidateFields(out string error)
    {
        if (Value <= 0)
        {
            error = "Value must be positive.";
            return false;
        }

        if (DiscountType == DiscountType.Percentage && Value > 100)
        {
            error = "A percentage discount cannot exceed 100.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_reasonEdit.Text))
        {
            error = "Reason is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    }
