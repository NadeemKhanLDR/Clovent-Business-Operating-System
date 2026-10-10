namespace Clovent.Restaurant.Application.QuickBooks;

/// <summary>Target QuickBooks deployment edition.</summary>
public enum QuickBooksEdition
{
    /// <summary>QuickBooks Desktop via COM / QBSDK x86 interop or qbXML messaging bridge.</summary>
    Desktop,

    /// <summary>QuickBooks Online cloud REST API via OAuth2 and JSON envelopes.</summary>
    Online,

    /// <summary>In-process simulation gateway with controllable latency and fault injection.</summary>
    Simulated
}

/// <summary>Configuration settings for QuickBooks connectivity.</summary>
public sealed class QuickBooksGatewayOptions
{
    /// <summary>Target edition (Desktop, Online, Simulated).</summary>
    public QuickBooksEdition Edition { get; set; } = QuickBooksEdition.Simulated;

    /// <summary>File path to the QuickBooks company file (.qbw) for Desktop edition.</summary>
    public string? CompanyFilePath { get; set; }

    /// <summary>API Base Endpoint URL for QuickBooks Online.</summary>
    public string ApiEndpoint { get; set; } = "https://quickbooks.api.intuit.com/v3/company";

    /// <summary>Intuit RealmId / Company ID for QuickBooks Online.</summary>
    public string? RealmId { get; set; }

    /// <summary>OAuth2 Access Token for QuickBooks Online API calls.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Default QuickBooks chart of accounts income account name for restaurant sales.</summary>
    public string DefaultIncomeAccount { get; set; } = "Food & Beverage Sales";

    /// <summary>Default QuickBooks asset account name for cash/clearing receipts.</summary>
    public string DefaultAssetAccount { get; set; } = "Undeposited Funds";

    /// <summary>Communication timeout in seconds (default 15s).</summary>
    public int TimeoutSeconds { get; set; } = 15;
}

/// <summary>Individual product or service line on a QuickBooks sales invoice.</summary>
public sealed record QuickBooksInvoiceLineItem(
    Guid ProductVariantId,
    string Sku,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string? TaxCode = null);

/// <summary>Structured payload for creating or reconciling a sales invoice in QuickBooks.</summary>
public sealed record QuickBooksInvoiceRequest(
    Guid OrderId,
    string OrderNumber,
    string? CustomerName,
    decimal SubTotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset TxnDate,
    IReadOnlyList<QuickBooksInvoiceLineItem> Lines,
    Guid? BranchId = null,
    Guid? TerminalId = null);

/// <summary>Structured payload for recording a customer payment in QuickBooks.</summary>
public sealed record QuickBooksPaymentRequest(
    Guid PaymentId,
    Guid OrderId,
    string OrderNumber,
    string PaymentMethod,
    decimal Amount,
    string? PaymentReference,
    DateTimeOffset PaymentDate,
    string? QuickBooksInvoiceTxnId = null);

/// <summary>Structured payload for recording daily shift cash/sales summaries in QuickBooks.</summary>
public sealed record QuickBooksShiftSummaryRequest(
    Guid ShiftId,
    int ShiftNumber,
    Guid TerminalId,
    string CashierName,
    decimal TotalSales,
    decimal CashTendered,
    decimal CashVariance,
    int OrderCount,
    DateTimeOffset ClosedAtUtc);

/// <summary>Status query response from QuickBooks for reconciliation checks.</summary>
public sealed record QuickBooksTransactionStatusDto(
    string TxnId,
    string Status,
    decimal Amount,
    DateTimeOffset? TxnDate,
    string? DocNumber);
