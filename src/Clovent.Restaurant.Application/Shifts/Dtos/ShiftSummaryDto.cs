using System;
using System.Collections.Generic;

namespace Clovent.Restaurant.Application.Shifts.Dtos;

/// <summary>Detailed financial breakdown and summary of a cash register shift session.</summary>
public sealed record ShiftSummaryDto(
    ShiftDto Shift,
    decimal StartingCash,
    decimal CashSales,
    decimal CardSales,
    decimal OtherSales,
    decimal TotalSales,
    decimal CashIn,
    decimal CashOut,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal Variance,
    int TotalTransactions,
    IReadOnlyList<CashMovementDto> CashMovements,
    decimal CashCollections = 0m,
    decimal CashRefunds = 0m,
    decimal CustomerAccountCreditRefunds = 0m,
    decimal OtherRefunds = 0m)
{
    /// <summary>Total refunds recorded during this shift session across all settlement methods.</summary>
    public decimal TotalRefunds => CashRefunds + CustomerAccountCreditRefunds + OtherRefunds;
}
