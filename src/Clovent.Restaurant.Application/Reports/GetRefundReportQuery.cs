using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Refunds.Dtos;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.Refunds;
using MediatR;

namespace Clovent.Restaurant.Application.Reports;

/// <summary>Summary totals and breakdown of refunds in a reporting period.</summary>
public sealed record RefundReportDto(
    Guid WarehouseId,
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalRefundsCount,
    decimal TotalGrossRefunded,
    decimal TotalDiscountReversed,
    decimal TotalTaxReversed,
    decimal TotalNetRefunded,
    decimal CashPayoutsTotal,
    decimal CardRefundsTotal,
    decimal CustomerCreditTotal,
    decimal MerchandiseCreditTotal,
    IReadOnlyList<RefundDto> Refunds);

/// <summary>Query to produce the comprehensive Refund Report.</summary>
public sealed record GetRefundReportQuery(
    Guid WarehouseId,
    DateOnly FromDate,
    DateOnly ToDate) : IRequest<RefundReportDto>;

/// <summary>Handles <see cref="GetRefundReportQuery"/>.</summary>
public sealed class GetRefundReportQueryHandler(IRefundRepository refundRepository)
    : IRequestHandler<GetRefundReportQuery, RefundReportDto>
{
    /// <inheritdoc/>
    public async Task<RefundReportDto> Handle(GetRefundReportQuery request, CancellationToken cancellationToken)
    {
        var warehouseId = new WarehouseId(request.WarehouseId);
        var allRefunds = await refundRepository.GetAllAsync(cancellationToken);

        bool MatchesDate(DateTimeOffset utc)
        {
            var d = DateOnly.FromDateTime(utc.UtcDateTime);
            return d >= request.FromDate && d <= request.ToDate;
        }

        var inRange = allRefunds
            .Where(r => r.WarehouseId == warehouseId && MatchesDate(r.RefundedAtUtc))
            .OrderByDescending(r => r.RefundedAtUtc)
            .ToList();

        decimal grossTotal = inRange.Sum(r => r.SubtotalRefunded);
        decimal discTotal = inRange.Sum(r => r.DiscountReversedTotal);
        decimal taxTotal = inRange.Sum(r => r.TaxReversedTotal);
        decimal netTotal = inRange.Sum(r => r.GrandTotalRefunded);

        decimal cashTotal = inRange
            .Where(r => r.SettlementMethod == RefundSettlementMethod.CashPayout)
            .Sum(r => r.GrandTotalRefunded);

        decimal cardTotal = inRange
            .Where(r => r.SettlementMethod == RefundSettlementMethod.ExternalCardRefund)
            .Sum(r => r.GrandTotalRefunded);

        decimal creditTotal = inRange
            .Where(r => r.SettlementMethod == RefundSettlementMethod.CustomerAccountCredit)
            .Sum(r => r.GrandTotalRefunded);

        decimal merchTotal = inRange
            .Where(r => r.SettlementMethod == RefundSettlementMethod.MerchandiseCredit)
            .Sum(r => r.GrandTotalRefunded);

        return new RefundReportDto(
            request.WarehouseId,
            request.FromDate,
            request.ToDate,
            inRange.Count,
            MoneyRoundingPolicy.RoundMoney(grossTotal),
            MoneyRoundingPolicy.RoundMoney(discTotal),
            MoneyRoundingPolicy.RoundMoney(taxTotal),
            MoneyRoundingPolicy.RoundMoney(netTotal),
            MoneyRoundingPolicy.RoundMoney(cashTotal),
            MoneyRoundingPolicy.RoundMoney(cardTotal),
            MoneyRoundingPolicy.RoundMoney(creditTotal),
            MoneyRoundingPolicy.RoundMoney(merchTotal),
            inRange.Select(RefundDto.FromDomain).ToList());
    }
}
