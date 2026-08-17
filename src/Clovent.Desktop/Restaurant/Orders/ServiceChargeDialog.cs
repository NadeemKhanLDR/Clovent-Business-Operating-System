using Clovent.Desktop.MasterData;
using Clovent.Restaurant.ServiceCharges;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Prompt for applying a service charge to an order - type, value, and reason. Control tree lives in <c>ServiceChargeDialog.Designer.cs</c>; this file holds behavior only.</summary>
public sealed partial class ServiceChargeDialog : MasterDataEditFormBase
{
    /// <summary>Builds the dialog.</summary>
    public ServiceChargeDialog() : base("Apply Service Charge")
    {
        InitializeComponent();
        }

    /// <summary>The selected service charge type.</summary>
    public ServiceChargeType ServiceChargeType => Enum.Parse<ServiceChargeType>((string)_typeCombo.SelectedItem);

    /// <summary>The entered value - a percentage (0-100) or a fixed amount, per <see cref="ServiceChargeType"/>.</summary>
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

        if (ServiceChargeType == ServiceChargeType.Percentage && Value > 100)
        {
            error = "A percentage service charge cannot exceed 100.";
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
