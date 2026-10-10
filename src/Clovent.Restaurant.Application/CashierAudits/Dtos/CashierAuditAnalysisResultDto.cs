using System;
using System.Collections.Generic;

namespace Clovent.Restaurant.Application.CashierAudits.Dtos;

/// <summary>Summary returned upon executing an automated cashier audit scan.</summary>
public sealed record CashierAuditAnalysisResultDto(
    int TotalShiftsScanned,
    int TotalOrdersScanned,
    int NewAlertsGenerated,
    int ExistingAlertsPreserved,
    IReadOnlyList<CashierAuditAlertDto> Alerts);
