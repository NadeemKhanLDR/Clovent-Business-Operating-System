using System;
using Clovent.Restaurant.AuditAlerts;

namespace Clovent.Restaurant.Application.CashierAudits.Dtos;

/// <summary>DTO representation of a durable cashier audit alert.</summary>
public sealed record CashierAuditAlertDto(
    Guid Id,
    Guid? ShiftId,
    Guid? OrderId,
    Guid? CashierId,
    string CashierName,
    string AnomalyType,
    string Severity,
    decimal RiskScore,
    string Description,
    string? SuspectDetailsJson,
    DateTimeOffset DetectedAtUtc,
    string Status,
    string? ReviewedBy,
    DateTimeOffset? ReviewedAtUtc,
    string? ResolutionNotes,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Maps from domain aggregate to DTO.</summary>
    public static CashierAuditAlertDto FromDomain(CashierAuditAlert alert) =>
        new(
            alert.Id.Value,
            alert.ShiftId?.Value,
            alert.OrderId?.Value,
            alert.CashierId?.Value,
            alert.CashierName,
            alert.AnomalyType.ToString(),
            alert.Severity.ToString(),
            alert.RiskScore,
            alert.Description,
            alert.SuspectDetailsJson,
            alert.DetectedAtUtc,
            alert.Status.ToString(),
            alert.ReviewedBy,
            alert.ReviewedAtUtc,
            alert.ResolutionNotes,
            alert.CreatedAtUtc);
}
