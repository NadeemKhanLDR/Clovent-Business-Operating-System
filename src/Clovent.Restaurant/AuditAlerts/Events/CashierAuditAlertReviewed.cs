using System;
using Clovent.Domain;

namespace Clovent.Restaurant.AuditAlerts.Events;

/// <summary>Raised when a store manager reviews an audit alert.</summary>
public sealed record CashierAuditAlertReviewed(
    CashierAuditAlertId AlertId,
    string ReviewedBy,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
