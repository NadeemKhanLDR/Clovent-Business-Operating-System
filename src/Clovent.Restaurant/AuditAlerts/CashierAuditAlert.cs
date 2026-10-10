using System;
using Clovent.Domain;
using Clovent.Identity.Users;
using Clovent.Restaurant.AuditAlerts.Events;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Shifts;

namespace Clovent.Restaurant.AuditAlerts;

/// <summary>
/// A durable audit anomaly record capturing high-risk cashier activity, shrinkage patterns,
/// void discrepancies, unlinked cash drawer activity, or abnormal discount spikes.
/// Persisted in <c>[Restaurant].[CashierAuditAlerts]</c> for manager review and forensic investigation.
/// </summary>
public sealed class CashierAuditAlert : AggregateRoot<CashierAuditAlertId>
{
    /// <summary>The POS shift session where the anomaly occurred, if applicable.</summary>
    public ShiftId? ShiftId { get; private set; }

    /// <summary>The suspect order identifier, if associated with a single order transaction.</summary>
    public OrderId? OrderId { get; private set; }

    /// <summary>The user account of the cashier responsible for the activity, if identified.</summary>
    public UserId? CashierId { get; private set; }

    /// <summary>The display name / username of the cashier.</summary>
    public string CashierName { get; private set; } = string.Empty;

    /// <summary>The behavioral anomaly pattern classification.</summary>
    public CashierAnomalyType AnomalyType { get; private set; }

    /// <summary>The urgency and financial risk level of this anomaly.</summary>
    public AuditAlertSeverity Severity { get; private set; }

    /// <summary>Calculated composite risk score on a 0.0 to 100.0 scale.</summary>
    public decimal RiskScore { get; private set; }

    /// <summary>Human-readable narrative describing the nature of the detected anomaly.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Serialized JSON details capturing suspect order numbers, amounts, timestamps, and lines.</summary>
    public string? SuspectDetailsJson { get; private set; }

    /// <summary>UTC timestamp when the analyzer engine identified this pattern.</summary>
    public DateTimeOffset DetectedAtUtc { get; private set; }

    /// <summary>Current manager investigation workflow status.</summary>
    public AuditAlertStatus Status { get; private set; }

    /// <summary>Username of the store manager who reviewed or dismissed this alert.</summary>
    public string? ReviewedBy { get; private set; }

    /// <summary>UTC timestamp when the alert was reviewed or dismissed.</summary>
    public DateTimeOffset? ReviewedAtUtc { get; private set; }

    /// <summary>Manager's resolution explanation, corrective action, or false-positive notes.</summary>
    public string? ResolutionNotes { get; private set; }

    /// <summary>UTC timestamp when this record was persisted.</summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Parameterless constructor for EF Core persistence.</summary>
    private CashierAuditAlert() { }

    /// <summary>Constructs an instance with all fields explicitly.</summary>
    private CashierAuditAlert(
        CashierAuditAlertId id,
        ShiftId? shiftId,
        OrderId? orderId,
        UserId? cashierId,
        string cashierName,
        CashierAnomalyType anomalyType,
        AuditAlertSeverity severity,
        decimal riskScore,
        string description,
        string? suspectDetailsJson,
        DateTimeOffset detectedAtUtc,
        AuditAlertStatus status,
        string? reviewedBy,
        DateTimeOffset? reviewedAtUtc,
        string? resolutionNotes,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        ShiftId = shiftId;
        OrderId = orderId;
        CashierId = cashierId;
        CashierName = cashierName;
        AnomalyType = anomalyType;
        Severity = severity;
        RiskScore = riskScore;
        Description = description;
        SuspectDetailsJson = suspectDetailsJson;
        DetectedAtUtc = detectedAtUtc;
        Status = status;
        ReviewedBy = reviewedBy;
        ReviewedAtUtc = reviewedAtUtc;
        ResolutionNotes = resolutionNotes;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new active cashier audit alert.</summary>
    public static CashierAuditAlert Create(
        ShiftId? shiftId,
        OrderId? orderId,
        UserId? cashierId,
        string cashierName,
        CashierAnomalyType anomalyType,
        AuditAlertSeverity severity,
        decimal riskScore,
        string description,
        string? suspectDetailsJson = null,
        DateTimeOffset? detectedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(cashierName))
            throw new ArgumentException("Cashier name is required.", nameof(cashierName));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Alert description is required.", nameof(description));

        var now = detectedAtUtc ?? DateTimeOffset.UtcNow;
        var clampedRiskScore = Math.Clamp(riskScore, 0.0m, 100.0m);

        var alert = new CashierAuditAlert(
            CashierAuditAlertId.New(),
            shiftId,
            orderId,
            cashierId,
            cashierName.Trim(),
            anomalyType,
            severity,
            clampedRiskScore,
            description.Trim(),
            suspectDetailsJson,
            now,
            AuditAlertStatus.Active,
            null,
            null,
            null,
            now);

        alert.AddDomainEvent(new CashierAuditAlertCreated(
            alert.Id,
            alert.CashierName,
            alert.AnomalyType,
            alert.Severity,
            alert.RiskScore,
            now));

        return alert;
    }

    /// <summary>Marks this alert as undergoing active manager investigation.</summary>
    public void MarkInvestigating(string? notes = null)
    {
        Status = AuditAlertStatus.Investigating;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            ResolutionNotes = string.IsNullOrWhiteSpace(ResolutionNotes)
                ? notes.Trim()
                : $"{ResolutionNotes}\n{notes.Trim()}";
        }
    }

    /// <summary>Marks this alert as reviewed by a store manager.</summary>
    public void Review(string reviewedBy, string resolutionNotes)
    {
        if (string.IsNullOrWhiteSpace(reviewedBy))
            throw new ArgumentException("Reviewer identity is required.", nameof(reviewedBy));

        Status = AuditAlertStatus.Reviewed;
        ReviewedBy = reviewedBy.Trim();
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        ResolutionNotes = resolutionNotes.Trim();

        AddDomainEvent(new CashierAuditAlertReviewed(Id, ReviewedBy, ReviewedAtUtc.Value));
    }

    /// <summary>Dismisses this alert as an approved exception or false positive.</summary>
    public void Dismiss(string dismissedBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(dismissedBy))
            throw new ArgumentException("Dismissed by is required.", nameof(dismissedBy));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Dismissal reason is required.", nameof(reason));

        Status = AuditAlertStatus.Dismissed;
        ReviewedBy = dismissedBy.Trim();
        ReviewedAtUtc = DateTimeOffset.UtcNow;
        ResolutionNotes = $"Dismissed: {reason.Trim()}";
    }
}
