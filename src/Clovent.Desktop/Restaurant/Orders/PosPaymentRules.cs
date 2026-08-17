namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Visual state of a single payment-method button in the POS tender strip.</summary>
public enum PaymentMethodButtonState
{
    /// <summary>Selectable, but not the current tender method.</summary>
    Unselected,

    /// <summary>The method the next recorded payment will use.</summary>
    Selected,

    /// <summary>Present on the strip but not currently usable.</summary>
    Unavailable,
}

/// <summary>
/// Presentation and completion decisions for the POS tender strip, kept apart from
/// <see cref="RestaurantPosForm"/> so both the form and its tests exercise the same rules
/// rather than a re-implementation.
/// </summary>
public static class PosPaymentRules
{
    /// <summary>
    /// Half-cent tolerance used whenever a balance is compared against zero, matching the
    /// rounding slack the rest of the POS applies to money.
    /// </summary>
    public const decimal BalanceEpsilon = 0.005m;

    /// <summary>Order statuses from which an order may still be completed.</summary>
    private static readonly string[] CompletableStatuses = ["Open", "Held"];

    /// <summary>Resolves how a payment-method button should render.</summary>
    public static PaymentMethodButtonState ResolveButtonState(bool isEnabled, bool isSelected) =>
        !isEnabled ? PaymentMethodButtonState.Unavailable
        : isSelected ? PaymentMethodButtonState.Selected
        : PaymentMethodButtonState.Unselected;

    /// <summary>A balance is settled once it is at or below zero within <see cref="BalanceEpsilon"/>.</summary>
    public static bool IsSettled(decimal balance) => balance <= BalanceEpsilon;

    /// <summary>
    /// Whether recording a payment should hand off to the order-completion workflow.
    /// <paramref name="paymentRecorded"/> must reflect a payment the server actually accepted,
    /// and <paramref name="balanceAfterPayment"/> the balance re-read afterwards - never the
    /// amount typed into the tender field - so a failed or merely-typed payment cannot complete
    /// an order. The status check keeps an already-completed or voided order from completing twice.
    /// </summary>
    public static bool ShouldAutoComplete(string? orderStatus, decimal balanceAfterPayment, bool paymentRecorded) =>
        paymentRecorded
        && orderStatus is not null
        && CompletableStatuses.Contains(orderStatus)
        && IsSettled(balanceAfterPayment);
}
