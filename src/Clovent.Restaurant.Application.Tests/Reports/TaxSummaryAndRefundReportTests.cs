using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Identity.Branches;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Reports;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Refunds;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Reports;

public class TaxSummaryAndRefundReportTests
{
    private readonly FakeOrderRepository _orderRepo = new();
    private readonly FakeOrderLineRepository _orderLineRepo = new();
    private readonly FakeRefundRepository _refundRepo = new();

    [Fact]
    public async Task GetTaxSummaryReportQuery_ComputesStatutoryTotalsAndBreakdownAccurately()
    {
        var warehouseId = WarehouseId.New();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Create completed Order 1 with Taxable and ZeroRated lines
        var order1 = Order.Create(OrderType.TakeAway, warehouseId);
        await _orderRepo.AddAsync(order1);

        var line1 = OrderLine.Create(order1.Id, ProductVariantId.New(), 2m, 500m, 16m, false);
        line1.SetTaxSnapshot("Taxable", "PRA", "PK-PRA-16", 900m, 100m, 144m, "1.3.0-AwayFromZero-v1");
        order1.AddOrderLine(line1.Id);
        await _orderLineRepo.AddAsync(line1);

        var line2 = OrderLine.Create(order1.Id, ProductVariantId.New(), 1m, 200m, 0m, false);
        line2.SetTaxSnapshot("ZeroRated", "FBR", "PK-FBR-ZERO", 200m, 0m, 0m, "1.3.0-AwayFromZero-v1");
        order1.AddOrderLine(line2.Id);
        await _orderLineRepo.AddAsync(line2);

        order1.AssignDailySalesNumber(1);
        order1.Complete();

        // 2. Create completed Order 2 with Exempt line
        var order2 = Order.Create(OrderType.TakeAway, warehouseId);
        await _orderRepo.AddAsync(order2);

        var line3 = OrderLine.Create(order2.Id, ProductVariantId.New(), 1m, 300m, 0m, false);
        line3.SetTaxSnapshot("Exempt", "FBR", "PK-FBR-EXEMPT", 300m, 0m, 0m, "1.3.0-AwayFromZero-v1");
        order2.AddOrderLine(line3.Id);
        await _orderLineRepo.AddAsync(line3);

        order2.AssignDailySalesNumber(2);
        order2.Complete();

        // 3. Create a Refund against order 1 line 1 (1 unit refund: 500 gross, 50 disc, 72 tax reversed)
        var refund = Refund.Create(
            RefundNumber.Generate(DateTimeOffset.UtcNow),
            order1.Id,
            BranchId.New(),
            warehouseId,
            Guid.NewGuid(),
            "Cashier",
            "Customer Return",
            "IDEM-REP-1",
            RefundSettlementMethod.CashPayout);

        var refLine = RefundLine.Create(
            refund.Id,
            line1.Id,
            line1.ProductVariantId,
            "SKU-1",
            "Item",
            1m,
            500m,
            50m,
            72m,
            522m,
            "Taxable",
            "PK-PRA-16",
            16m,
            false,
            InventoryDisposition.Restock);
        refund.AddLine(refLine);
        refund.FinalizeFinancials(500m, 50m, 72m, 522m, "{}");
        await _refundRepo.AddAsync(refund);

        var queryHandler = new GetTaxSummaryReportQueryHandler(_orderRepo, _orderLineRepo, _refundRepo);
        var report = await queryHandler.Handle(
            new GetTaxSummaryReportQuery(warehouseId.Value, today.AddDays(-1), today.AddDays(1)),
            CancellationToken.None);

        Assert.NotNull(report);
        // Gross sales = 1000 + 200 + 300 = 1500
        Assert.Equal(1500.00m, report.GrossSalesBeforeDiscounts);
        Assert.Equal(100.00m, report.TotalDiscounts);
        Assert.Equal(1400.00m, report.SalesExcludingTax);
        Assert.Equal(900.00m, report.TaxableSalesBase);
        Assert.Equal(200.00m, report.ZeroRatedSales);
        Assert.Equal(300.00m, report.ExemptSales);
        Assert.Equal(144.00m, report.TotalTaxCharged);
        Assert.Equal(72.00m, report.TotalTaxRefunded);
        Assert.Equal(72.00m, report.NetTaxPayable); // 144 - 72

        var praItem = report.TaxBreakdown.FirstOrDefault(b => b.TaxCode == "PK-PRA-16");
        Assert.NotNull(praItem);
        Assert.Equal(144.00m, praItem.TaxCharged);
        Assert.Equal(72.00m, praItem.TaxRefunded);
        Assert.Equal(72.00m, praItem.NetTax);
    }

    [Fact]
    public async Task GetRefundReportQuery_AggregatesSettlementTendersAccurately()
    {
        var warehouseId = WarehouseId.New();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Refund 1: CashPayout of 200
        var r1 = Refund.Create(
            RefundNumber.Generate(DateTimeOffset.UtcNow), OrderId.New(), BranchId.New(), warehouseId,
            Guid.NewGuid(), "Cashier", "R1", "IDEM-R1", RefundSettlementMethod.CashPayout);
        r1.FinalizeFinancials(200m, 0m, 0m, 200m, "{}");
        await _refundRepo.AddAsync(r1);

        // Refund 2: ExternalCardRefund of 300
        var r2 = Refund.Create(
            RefundNumber.Generate(DateTimeOffset.UtcNow), OrderId.New(), BranchId.New(), warehouseId,
            Guid.NewGuid(), "Cashier", "R2", "IDEM-R2", RefundSettlementMethod.ExternalCardRefund);
        r2.FinalizeFinancials(300m, 0m, 0m, 300m, "{}");
        await _refundRepo.AddAsync(r2);

        // Refund 3: CustomerAccountCredit of 150
        var r3 = Refund.Create(
            RefundNumber.Generate(DateTimeOffset.UtcNow), OrderId.New(), BranchId.New(), warehouseId,
            Guid.NewGuid(), "Cashier", "R3", "IDEM-R3", RefundSettlementMethod.CustomerAccountCredit);
        r3.FinalizeFinancials(150m, 0m, 0m, 150m, "{}");
        await _refundRepo.AddAsync(r3);

        var queryHandler = new GetRefundReportQueryHandler(_refundRepo);
        var report = await queryHandler.Handle(
            new GetRefundReportQuery(warehouseId.Value, today.AddDays(-1), today.AddDays(1)),
            CancellationToken.None);

        Assert.Equal(3, report.TotalRefundsCount);
        Assert.Equal(650.00m, report.TotalGrossRefunded);
        Assert.Equal(650.00m, report.TotalNetRefunded);
        Assert.Equal(200.00m, report.CashPayoutsTotal);
        Assert.Equal(300.00m, report.CardRefundsTotal);
        Assert.Equal(150.00m, report.CustomerCreditTotal);
        Assert.Equal(0.00m, report.MerchandiseCreditTotal);
    }

    [Fact]
    public async Task GetTaxSummaryReportQuery_PreservesSelectedNonPraTaxAuthoritySnapshots_OnSalesAndRefunds()
    {
        var warehouseId = WarehouseId.New();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var order = Order.Create(OrderType.TakeAway, warehouseId);
        await _orderRepo.AddAsync(order);

        var srbLine = OrderLine.Create(
            order.Id,
            ProductVariantId.New(),
            2m,
            1000m,
            13m,
            false,
            taxClassification: "Taxable",
            taxAuthority: "SRB",
            taxCode: "PK-SRB-13");
        srbLine.SetTaxSnapshot("Taxable", "SRB", "PK-SRB-13", 2000m, 0m, 260m, "1.3.0-AwayFromZero-v1");
        order.AddOrderLine(srbLine.Id);
        await _orderLineRepo.AddAsync(srbLine);

        order.AssignDailySalesNumber(1);
        order.Complete();

        var refund = Refund.Create(
            RefundNumber.Generate(DateTimeOffset.UtcNow),
            order.Id,
            BranchId.New(),
            warehouseId,
            Guid.NewGuid(),
            "Cashier",
            "Partial SRB Return",
            "IDEM-SRB-1",
            RefundSettlementMethod.CashPayout);
        var refLine = RefundLine.Create(
            refund.Id,
            srbLine.Id,
            srbLine.ProductVariantId,
            "SKU-SRB",
            "Karachi Platter",
            1m,
            1000m,
            0m,
            130m,
            1130m,
            "Taxable",
            "PK-SRB-13",
            13m,
            false,
            InventoryDisposition.Restock);
        refund.AddLine(refLine);
        refund.FinalizeFinancials(1000m, 0m, 130m, 1130m, "{}");
        await _refundRepo.AddAsync(refund);

        var queryHandler = new GetTaxSummaryReportQueryHandler(_orderRepo, _orderLineRepo, _refundRepo);
        var report = await queryHandler.Handle(
            new GetTaxSummaryReportQuery(warehouseId.Value, today.AddDays(-1), today.AddDays(1)),
            CancellationToken.None);

        var srbItem = Assert.Single(report.TaxBreakdown);
        Assert.Equal("SRB", srbItem.Authority);
        Assert.Equal("PK-SRB-13", srbItem.TaxCode);
        Assert.Equal(260.00m, srbItem.TaxCharged);
        Assert.Equal(130.00m, srbItem.TaxRefunded);
        Assert.Equal(130.00m, srbItem.NetTax);
    }
}
