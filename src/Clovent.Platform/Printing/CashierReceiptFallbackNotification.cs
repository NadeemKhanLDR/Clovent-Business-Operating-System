namespace Clovent.Platform.Printing;

/// <summary>
/// Event payload for notifying cashier terminals when a receipt print dispatch has been diverted
/// to the durable quarantine buffer due to hardware fault or open circuit breaker,
/// without aborting the completed sale transaction or payment tender.
/// </summary>
public sealed record CashierReceiptFallbackNotification(
    Guid JobId,
    string CorrelationId,
    string PrinterName,
    string Reason,
    DateTimeOffset QuarantinedAtUtc);

/// <summary>
/// Summary report of batch re-spooling attempts against quarantined print jobs.
/// </summary>
public sealed record QuarantineReplaySummary(
    int TotalAttempted,
    int SuccessfullyRecovered,
    int StillFailing);
