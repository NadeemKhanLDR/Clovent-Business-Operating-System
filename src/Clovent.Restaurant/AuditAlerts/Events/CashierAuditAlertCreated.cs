using System;
using Clovent.Domain;

namespace Clovent.Restaurant.AuditAlerts.Events;

/// <summary>Raised when a high-risk cashier audit anomaly is detected and surfaced.</summary>
public sealed record CashierAuditAlertCreated(
    CashierAuditAlertId AlertId,
    string CashierName,
    CashierAnomalyType AnomalyType,
    AuditAlertSeverity Severity,
    decimal RiskScore,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
