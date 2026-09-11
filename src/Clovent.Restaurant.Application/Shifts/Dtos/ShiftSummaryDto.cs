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
    IReadOnlyList<CashMovementDto> CashMovements);
