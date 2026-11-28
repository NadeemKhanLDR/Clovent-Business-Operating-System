namespace Clovent.Restaurant.DomainServices;

/// <summary>
/// Canonical classifier for POS payment-method names and aliases, normalizing
/// whitespace and casing across payment recording, voids, refunds, shift
/// reconciliation, customer receivables, and reports.
/// </summary>
public static class PaymentMethodClassifier
{
    /// <summary>Determines whether the payment method represents physical cash.</summary>
    public static bool IsCash(string? paymentMethodName) =>
        string.Equals(paymentMethodName?.Trim(), "Cash", StringComparison.OrdinalIgnoreCase);

    /// <summary>Determines whether the payment method represents customer credit / On-Account settlement.</summary>
    public static bool IsOnAccountCredit(string? paymentMethodName)
    {
        var normalized = paymentMethodName?.Trim();
        return string.Equals(normalized, "On Account", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Customer Account", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Credit", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Customer Credit", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Determines whether the payment method consumes pre-collected customer advance balance.</summary>
    public static bool IsCustomerAdvance(string? paymentMethodName)
    {
        var normalized = paymentMethodName?.Trim();
        return string.Equals(normalized, "Customer Advance", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Advance", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the payment method represents a card tender
    /// (e.g. "Card", "Credit Card", "Debit Card", "MasterCard"), excluding
    /// customer credit/advance methods and gift cards.
    /// </summary>
    public static bool IsCard(string? paymentMethodName)
    {
        var normalized = paymentMethodName?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        if (IsOnAccountCredit(normalized) ||
            IsCustomerAdvance(normalized) ||
            string.Equals(normalized, "Gift Card", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Only established card aliases qualify. Names such as Discarded or
        // Gift Card Voucher must not silently become bank-card collections.
        return string.Equals(normalized, "Card", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Credit Card", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Debit Card", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "MasterCard", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Visa Card", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the payment method represents a direct bank transfer, wire, or electronic funds transfer.
    /// </summary>
    public static bool IsBankTransfer(string? paymentMethodName)
    {
        var normalized = paymentMethodName?.Trim();
        if (string.IsNullOrEmpty(normalized)) return false;

        return string.Equals(normalized, "Bank Transfer", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Bank", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Wire Transfer", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Direct Deposit", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "IBFT", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "EFT", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the payment method represents a digital or mobile wallet payment (e.g. JazzCash, EasyPaisa, SadaPay, QR).
    /// </summary>
    public static bool IsDigitalPayment(string? paymentMethodName)
    {
        var normalized = paymentMethodName?.Trim();
        if (string.IsNullOrEmpty(normalized)) return false;

        return string.Equals(normalized, "Digital Payment", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Digital Wallet", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Mobile Wallet", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "JazzCash", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "EasyPaisa", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "SadaPay", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "NayaPay", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Raast", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "QR Payment", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "UPI", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(normalized, "Online", StringComparison.OrdinalIgnoreCase);
    }
}
