using Clovent.Platform.Execution;
using System.Reflection;
using System.Text.Json;
using Clovent.Catalog.Variants;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.DayClose.Queries;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using Clovent.Restaurant.Application.Reports;
using Clovent.Restaurant.Application.Shifts.Queries;
using Clovent.Restaurant.Application.Shifts.Services;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Refunds;
using Clovent.Restaurant.Shifts;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Reports;

// Synthetic persisted facts, not commissioning or command/persistence qualification.
// Expectations below are literal independently calculated amounts.
public sealed class TransactionReportingAuditTests
{
    [Fact]
    public async Task MixedDiscountedSale_BillKpiMatchesRowsAndTaxSummary()
    {
        var d = new Data();
        d.Sale([new(2m, 100m, 10m, false, 20m, 180m, 18m), new(1m, 50m, 0m, false, 5m, 45m, 0m, "Exempt")]);
        var expanded = await d.Expanded(d.Day, d.Day);
        var tax = await d.Tax(d.Day, d.Day);
        Assert.Equal(250m, expanded.Kpis.GrossSales);
        Assert.Equal(25m, expanded.Kpis.Discounts);
        Assert.Equal(225m, expanded.Kpis.NetSales);
        Assert.Equal(18m, expanded.Kpis.Tax);
        Assert.Equal(243m, Assert.Single(expanded.Orders).Total);
        Assert.Equal(243m, expanded.Kpis.TotalBillSales);
        Assert.Equal(expanded.Orders.Sum(o => o.Total), expanded.Kpis.TotalBillSales);
        Assert.Equal(243m, expanded.OrderTypes.Sum(o => o.TotalSales));
        Assert.Equal(243m, expanded.Kpis.CashCollected);
        Assert.Equal(225m, tax.SalesExcludingTax);
        Assert.Equal(18m, tax.TotalTaxCharged);
        Assert.Equal(45m, tax.ExemptSales);
    }

    [Fact]
    public async Task DetailTaxIsAnAmount_NotARateOrAServiceCharge()
    {
        var d = new Data();
        d.Sale([new(2m, 100m, 10m, false, 20m, 180m, 18m)]);
        var report = await d.Expanded(d.Day, d.Day);
        var line = Assert.Single(Assert.Single(report.Orders).Lines!);
        Assert.Equal(18m, line.Tax);
        Assert.Equal(0m, line.ServiceCharge);
    }

    [Fact]
    public async Task CreatedYesterdayCompletedToday_IsCountedOnlyOnCompletionBusinessDate()
    {
        var d = new Data();
        d.Sale([new(1m, 100m, 0m, false, 0m, 100m, 0m, "Exempt")], created: d.At.AddDays(-1));
        Assert.Empty((await d.Expanded(d.Day.AddDays(-1), d.Day.AddDays(-1))).Orders);
        Assert.Single((await d.Expanded(d.Day, d.Day)).Orders);
        Assert.Single((await d.Expanded(d.Day.AddDays(-1), d.Day)).Orders);
    }

    [Fact]
    public async Task SplitTender_VoidedCashIsExcluded_AndShiftUsesCanonicalCardClassification()
    {
        var d = new Data();
        var customer = d.Customer();
        var shift = d.Shift();
        d.Sale([new(1m, 300m, 0m, false, 0m, 300m, 0m, "Exempt")],
            [("Cash", 20m, true), ("Cash", 40m, false), ("MasterCard", 60m, false), ("On Account", 200m, false)], customer, shift: shift);
        d.Entry(customer, 200m, 0m, 200m, "On Account", "On Account Sale", d.At);
        var expanded = await d.Expanded(d.Day, d.Day);
        Assert.Equal(300m, expanded.Kpis.TotalBillSales);
        Assert.Equal(100m, expanded.Kpis.TotalCollected);
        Assert.Equal(40m, expanded.Kpis.CashCollected);
        Assert.Equal(60m, expanded.Kpis.CardCollected);
        Assert.Equal(200m, expanded.Kpis.OnAccountCreated);
        var shiftReport = await new GetShiftSummaryQueryHandler(d.Shifts, d.Payments, d.Methods, d.Ledger, d.Refunds)
            .Handle(new(shift.Id.Value), default);
        Assert.Equal(40m, shiftReport.CashSales);
        Assert.Equal(60m, shiftReport.CardSales);
        Assert.Equal(200m, shiftReport.OtherSales);
        Assert.Equal(40m, shiftReport.ExpectedCash);
        var day = await new GetBusinessDaySummaryQueryHandler(new FakeBusinessDayCloseRepository(), d.Shifts,
            d.Payments, d.Methods, d.Dates, d.Orders, d.Lines, d.Refunds).Handle(new(d.Branch.Value, d.Day), default);
        Assert.Equal(300m, day.TotalSales);
        Assert.Equal(40m, day.CashSales);
        Assert.Equal(60m, day.CardSales);
        var ar = await d.Receivables(d.At.AddHours(1));
        Assert.Equal(200m, ar.TotalReceivables);
        Assert.Equal(200m, Assert.Single(ar.Rows).CurrentBalance);
    }

    [Fact]
    public async Task AdvanceSettlementAndVoidRestoreBalance_WithoutInventingNewCollection()
    {
        var d = new Data();
        var customer = d.Customer();
        d.Entry(customer, 0m, 300m, -300m, "Cash", "Customer Advance Payment (Cash)", d.At.AddDays(-1));
        d.Sale([new(1m, 100m, 0m, false, 0m, 100m, 0m, "Exempt")],
            [("Customer Advance", 100m, true), ("Cash", 100m, false)], customer);
        d.Entry(customer, 100m, 0m, -200m, "Customer Advance", "Customer Advance Settlement (AUDIT)", d.At);
        d.Entry(customer, 0m, 100m, -300m, null, "Void Advance Settlement (AUDIT)", d.At.AddMinutes(1), "VOID-AUDIT");
        var report = await d.Expanded(d.Day, d.Day);
        Assert.Equal(100m, report.Kpis.CashCollected);
        Assert.Equal(0m, report.Kpis.CustomerPaymentsCollected);
        Assert.Equal(0m, report.Kpis.CustomerAdvancesReceived);
        Assert.Equal(0m, report.Kpis.CustomerAdvancesApplied);
        var ar = await d.Receivables(d.At.AddHours(1));
        Assert.Equal(300m, ar.TotalAdvances);
        Assert.Equal(-300m, ar.NetPosition);
    }

    [Fact]
    public async Task AsOfStatement_DoesNotExposeFutureLastTransactionDate()
    {
        var d = new Data();
        var customer = d.Customer();
        d.Entry(customer, 100m, 0m, 100m, "On Account", "On Account Sale", d.At.AddDays(-1));
        d.Entry(customer, 0m, 60m, 40m, "Cash", "Customer Payment", d.At.AddDays(1));
        var report = await d.Receivables(d.At);
        var row = Assert.Single(report.Rows);
        Assert.Equal(100m, row.CurrentBalance);
        Assert.Equal(d.At.AddDays(-1), row.LastTransactionDate);
    }

    [Fact]
    public async Task InclusiveSaleAndRefundOnDifferentDates_ReconcileWithoutMovingOriginalSale()
    {
        var d = new Data();
        var order = d.Sale([new(2m, 55m, 10m, true, 0m, 100m, 10m)]);
        d.Refund(order, 1m, 55m, 0m, 5m, 55m, true, d.At.AddDays(1));
        var first = await d.Tax(d.Day, d.Day);
        var second = await d.Tax(d.Day.AddDays(1), d.Day.AddDays(1));
        Assert.Equal(110m, first.GrossSalesBeforeDiscounts);
        Assert.Equal(100m, first.SalesExcludingTax);
        Assert.Equal(10m, first.TotalTaxCharged);
        Assert.Equal(0m, first.TotalTaxRefunded);
        Assert.Equal(0m, second.GrossSalesBeforeDiscounts);
        Assert.Equal(5m, second.TotalTaxRefunded);
        Assert.Equal(-5m, second.NetTaxPayable);
        Assert.Equal(-50m, second.NetSales);
        var refund = await new GetRefundReportQueryHandler(d.Refunds).Handle(new(d.Warehouse.Value, d.Day.AddDays(1), d.Day.AddDays(1)), default);
        Assert.Equal(55m, refund.TotalNetRefunded);
        Assert.Equal(55m, refund.CashPayoutsTotal);
        Assert.Empty((await d.Expanded(d.Day.AddDays(1), d.Day.AddDays(1))).Orders);
        Assert.Equal(55m, (await d.Expanded(d.Day.AddDays(1), d.Day.AddDays(1))).Kpis.TotalRefunds);
    }

    [Fact]
    public async Task FractionalInclusiveRefunds_UsePersistedResidualsAndKeepSaleImmutable()
    {
        var d = new Data();
        var order = d.Sale([new(1m, 10.05m, 10m, true, 0m, 9.14m, .91m)]);
        var original = order.ReceiptSnapshotJson;
        d.Refund(order, .5m, 10.05m, 0m, .46m, 5.03m, true, d.At.AddMinutes(1));
        d.Refund(order, .5m, 10.05m, 0m, .45m, 5.02m, true, d.At.AddMinutes(2));
        var tax = await d.Tax(d.Day, d.Day);
        var refunds = await new GetRefundReportQueryHandler(d.Refunds).Handle(new(d.Warehouse.Value, d.Day, d.Day), default);
        Assert.Equal(10.05m, refunds.TotalGrossRefunded);
        Assert.Equal(10.05m, refunds.TotalNetRefunded);
        Assert.Equal(.91m, refunds.TotalTaxReversed);
        Assert.Equal(0m, tax.NetTaxPayable);
        Assert.Equal(0m, tax.NetSales);
        Assert.Equal(original, order.ReceiptSnapshotJson);
    }

    [Fact]
    public async Task DiscountedExclusivePartialRefund_ReversesOnlyPersistedShare()
    {
        var d = new Data();
        var order = d.Sale([new(2m, 100m, 10m, false, 20m, 180m, 18m)]);
        d.Refund(order, 1m, 100m, 10m, 9m, 99m, false, d.At);
        var tax = await d.Tax(d.Day, d.Day);
        var refunds = await new GetRefundReportQueryHandler(d.Refunds).Handle(new(d.Warehouse.Value, d.Day, d.Day), default);
        Assert.Equal(100m, refunds.TotalGrossRefunded);
        Assert.Equal(10m, refunds.TotalDiscountReversed);
        Assert.Equal(9m, refunds.TotalTaxReversed);
        Assert.Equal(99m, refunds.TotalNetRefunded);
        Assert.Equal(90m, tax.NetSales);
        Assert.Equal(9m, tax.NetTaxPayable);
    }

    [Fact]
    public async Task ExplicitBusinessZoneAndUtcTaxDate_AreDifferentContractsAtMidnight()
    {
        var d = new Data(new DateTimeOffset(2026, 1, 10, 19, 30, 0, TimeSpan.Zero));
        d.Dates.SetBusinessTimeZone(TimeZoneInfo.CreateCustomTimeZone("Audit UTC+5", TimeSpan.FromHours(5), "Audit UTC+5", "Audit UTC+5"));
        var order = d.Sale([new(1m, 100m, 0m, false, 0m, 100m, 0m, "Exempt")]);
        Assert.Empty((await d.Expanded(d.Day, d.Day)).Orders);
        Assert.Single((await d.Expanded(d.Day.AddDays(1), d.Day.AddDays(1))).Orders);
        Assert.Equal(100m, (await d.Tax(d.Day, d.Day)).GrossSalesBeforeDiscounts);
        Assert.Equal(0m, (await d.Tax(d.Day.AddDays(1), d.Day.AddDays(1))).GrossSalesBeforeDiscounts);
        Assert.Single((await d.Expanded(d.Day, d.Day.AddDays(1))).Orders);
    }

    [Fact]
    public async Task OperationalUpdateAfterCompletion_DoesNotMoveFrozenSaleToAnotherDay()
    {
        var d = new Data();
        var order = d.Sale([new(1m, 100m, 0m, false, 0m, 100m, 0m, "Exempt")]);
        var snapshot = order.ReceiptSnapshotJson;
        order.AssignRider("Synthetic rider");
        Data.Stamp(order, "UpdatedAtUtc", d.At.AddDays(1));
        Assert.Equal(snapshot, order.ReceiptSnapshotJson);
        Assert.Single((await d.Expanded(d.Day, d.Day)).Orders);
        Assert.Empty((await d.Expanded(d.Day.AddDays(1), d.Day.AddDays(1))).Orders);
        Assert.Equal(100m, (await d.Tax(d.Day, d.Day)).GrossSalesBeforeDiscounts);
        var daily = await new GetEndOfDayReportQueryHandler(d.Orders, d.Lines, d.Payments, d.Methods)
            .Handle(new(d.Warehouse.Value, d.Day, d.Day), default);
        Assert.Equal(100m, daily.TotalSales);
        Assert.Equal(d.At, Assert.Single(daily.Bills).CompletedAtUtc);
    }

    [Fact]
    public async Task WarehouseAndStatusFilters_ExcludeForeignCancelledAndVoidedSales()
    {
        var d = new Data();
        d.Sale([new(1m, 100m, 0m, false, 0m, 100m, 0m)]);
        d.Sale([new(1m, 900m, 0m, false, 0m, 900m, 0m)], warehouse: WarehouseId.New());
        var cancelled = Order.Create(OrderType.TakeAway, d.Warehouse);
        cancelled.Cancel("Synthetic cancelled draft");
        Data.Stamp(cancelled, "UpdatedAtUtc", d.At);
        d.Orders.Add(cancelled);
        var voided = Order.Create(OrderType.TakeAway, d.Warehouse);
        voided.Void("Synthetic voided draft");
        Data.Stamp(voided, "UpdatedAtUtc", d.At);
        d.Orders.Add(voided);
        var report = await d.Expanded(d.Day, d.Day);
        Assert.Single(report.Orders);
        Assert.Equal(100m, report.Kpis.TotalBillSales);
    }

    [Fact]
    public async Task ContinuityReplayTodayForYesterdaysSale_AttributesToFrozenOfflineDateAndTrueCalculationBase()
    {
        var d = new Data();
        var saleAt = d.At;
        var replayedAt = d.At.AddDays(1);
        var cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        d.Methods.Add(cashMethod);

        var txId = Guid.NewGuid();
        const decimal total = 10.05m;
        const string terminal = "POS-01";
        const string cashier = "Offline Cashier";
        var checksum = Clovent.Restaurant.Continuity.EmergencyTransaction.ComputeChecksum(
            txId, 1L, saleAt, terminal, d.Branch.Value, d.Warehouse.Value, cashier, total, "Cash");
        var hmac = Clovent.Restaurant.Continuity.EmergencyTransaction.ComputeHmacSignature(
            txId, 1L, Clovent.Restaurant.Continuity.EmergencyTransaction.GenesisHash,
            saleAt, terminal, d.Branch.Value, d.Warehouse.Value, cashier, total, "Cash");

        var offlineTx = new Clovent.Restaurant.Continuity.EmergencyTransaction
        {
            TransactionId = txId,
            SequenceNumber = 1L,
            PreviousTransactionHash = Clovent.Restaurant.Continuity.EmergencyTransaction.GenesisHash,
            TimestampUtc = saleAt,
            TerminalId = terminal,
            BranchId = d.Branch.Value,
            WarehouseId = d.Warehouse.Value,
            CashierId = Guid.NewGuid(),
            CashierName = cashier,
            OrderSnapshot = new Clovent.Restaurant.Continuity.EmergencyOrderSnapshot(
                "TakeAway",
                null,
                null,
                [new(Guid.NewGuid(), "INC-10", "Inclusive item", 1m, 10.05m, 10.05m, null, 10m, 0.91m, true, 0m)],
                10.05m, 0.91m, 0m, 0m, 10.05m, null, null),
            PaymentType = "Cash",
            AmountTendered = 10.05m,
            ChangeGiven = 0m,
            Checksum = checksum,
            HmacSignature = hmac
        };

        var journal = new FakeContinuityJournalStore();
        await journal.AppendAsync(offlineTx);

        var replayer = new Clovent.Restaurant.Application.Continuity.EmergencyJournalReplayer(
            journal,
            d.Orders,
            d.Lines,
            d.Payments,
            d.Methods,
            new FakeUnitOfWork(),
            new FakeOutboxRepository());

        var result = await replayer.ReplayPendingAsync();
        Assert.Equal(1, result.SuccessCount);

        var replayedOrder = Assert.Single(await d.Orders.GetAllAsync());
        // Simulate persistence stamping UpdatedAtUtc on replay day (Jan 11) while offline sale occurred on Jan 10.
        Data.Stamp(replayedOrder, "CreatedAtUtc", replayedAt);
        Data.Stamp(replayedOrder, "UpdatedAtUtc", replayedAt);

        var snap = JsonSerializer.Deserialize<ReceiptSnapshot>(replayedOrder.ReceiptSnapshotJson!)!;
        Assert.Equal(9.14m, snap.TotalTaxableBase);
        Assert.Equal(saleAt, snap.CompletedAtUtc);

        Assert.Single((await d.Expanded(d.Day, d.Day)).Orders);
        Assert.Empty((await d.Expanded(d.Day.AddDays(1), d.Day.AddDays(1))).Orders);
        Assert.Equal(10.05m, (await d.Tax(d.Day, d.Day)).GrossSalesBeforeDiscounts);
        Assert.Equal(0m, (await d.Tax(d.Day.AddDays(1), d.Day.AddDays(1))).GrossSalesBeforeDiscounts);
    }

    private sealed record Line(decimal Quantity, decimal Price, decimal Rate, bool Inclusive, decimal Discount, decimal Base, decimal Tax, string Classification = "Taxable");
    private sealed class Data
    {
        internal readonly DateTimeOffset At;
        internal readonly Guid Company = Guid.NewGuid();
        internal readonly Clovent.Platform.Execution.ExecutionContextAccessor Scope = new();
        internal Data(DateTimeOffset? at = null)
        {
            At = at ?? new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero);
            Scope.BeginScope(new Clovent.Platform.Execution.PlatformExecutionContext { OrganizationId = Guid.NewGuid(), CompanyId = Company });
        }
        internal DateOnly Day => DateOnly.FromDateTime(At.UtcDateTime);
        internal readonly WarehouseId Warehouse = WarehouseId.New();
        internal readonly BranchId Branch = BranchId.New();
        internal readonly BusinessDateProvider Dates = new(TimeZoneInfo.Utc);
        internal readonly FakeOrderRepository Orders = new();
        internal readonly FakeOrderLineRepository Lines = new();
        internal readonly FakePaymentRepository Payments = new();
        internal readonly FakePaymentMethodRepository Methods = new();
        internal readonly FakeCustomerRepository Customers = new();
        internal readonly FakeCustomerLedgerEntryRepository Ledger = new();
        internal readonly FakeRefundRepository Refunds = new();
        internal readonly FakeShiftRepository Shifts = new();
        private readonly Dictionary<string, PaymentMethod> _methods = [];
        private int _sequence;

        internal static void Stamp(object value, string name, object date)
        {
            var property = value.GetType().GetProperty(name)!;
            if (property.SetMethod is not null) property.SetValue(value, date);
            else value.GetType().GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(value, date);
        }

        internal Customer Customer()
        {
            var customer = Clovent.Restaurant.Customers.Customer.Create(EntityCode.Create("AUD-" + Guid.NewGuid().ToString("N")[..8]), "Synthetic customer", "0000000000", "Synthetic address", null, 0m, 1000m, null);
            Stamp(customer, "CreatedAtUtc", At.AddDays(-10));
            customer.AssignCompany(Company);
            Customers.Add(customer);
            return customer;
        }

        internal Shift Shift()
        {
            var shift = Clovent.Restaurant.Shifts.Shift.Open(++_sequence, Branch, Warehouse, TerminalId.New(), UserId.New(), "Synthetic cashier", 0m);
            Stamp(shift, "OpenedAtUtc", At);
            Shifts.AddAsync(shift).GetAwaiter().GetResult();
            return shift;
        }

        internal void Entry(Customer customer, decimal debit, decimal credit, decimal balance, string? method, string description, DateTimeOffset date, string reference = "AUDIT")
        {
            var entry = CustomerLedgerEntry.Create(customer.Id, reference, description, debit, credit, balance, paymentMethod: method, date: date);
            entry.AssignCompany(Company);
            Ledger.Add(entry);
        }

        internal Order Sale(Line[] facts, (string Method, decimal Amount, bool Voided)[]? tenders = null,
            Customer? customer = null, DateTimeOffset? created = null, WarehouseId? warehouse = null, Shift? shift = null)
        {
            var order = Order.Create(OrderType.TakeAway, warehouse ?? Warehouse);
            if (customer is not null) order.SetCustomer(customer.Id);
            var items = new List<ReceiptSnapshotItem>();
            foreach (var fact in facts)
            {
                var line = OrderLine.Create(order.Id, ProductVariantId.New(), fact.Quantity, fact.Price, fact.Rate, fact.Inclusive);
                line.SetTaxSnapshot(fact.Classification, "PRA", fact.Rate > 0 ? "AUDIT-TAX" : "AUDIT-EXEMPT", fact.Base, fact.Discount, fact.Tax, "AUDIT");
                order.AddOrderLine(line.Id);
                Lines.Add(line);
                items.Add(new(line.ProductVariantId.Value, "AUDIT", "Synthetic item", fact.Quantity, fact.Price, fact.Quantity * fact.Price, null,
                    fact.Classification, line.TaxCode, fact.Rate, fact.Inclusive, fact.Base, fact.Tax, fact.Discount));
            }
            var gross = facts.Sum(l => l.Quantity * l.Price);
            var discount = facts.Sum(l => l.Discount);
            var exclusive = facts.Where(l => !l.Inclusive).Sum(l => l.Tax);
            var payable = gross - discount + exclusive;
            var receiptPayments = new List<ReceiptSnapshotPayment>();
            foreach (var tender in tenders ?? [("Cash", payable, false)])
            {
                if (!_methods.TryGetValue(tender.Method, out var method))
                {
                    method = PaymentMethod.Create(PaymentMethodName.Create(tender.Method));
                    Methods.Add(method);
                    _methods[tender.Method] = method;
                }
                var payment = Payment.Create(order.Id, method.Id, tender.Amount, shift?.Id);
                Stamp(payment, "CreatedAtUtc", At);
                order.RecordPayment(payment.Id);
                if (tender.Voided) payment.Void();
                else receiptPayments.Add(new(tender.Amount, tender.Method));
                Payments.Add(payment);
            }
            var snapshot = new ReceiptSnapshot(order.Id.Value, order.OrderNumber.Value, null, "TakeAway", At,
                items, gross, facts.Sum(l => l.Tax), discount, 0m, payable, 0m, receiptPayments, "Synthetic cashier", "SYNTHETIC", null,
                TotalTaxableBase: facts.Sum(l => l.Base), TotalExclusiveTax: exclusive, TotalInclusiveTax: facts.Where(l => l.Inclusive).Sum(l => l.Tax));
            order.SetReceiptSnapshot(JsonSerializer.Serialize(snapshot));
            order.Complete();
            Stamp(order, "CreatedAtUtc", created ?? At);
            Stamp(order, "UpdatedAtUtc", At);
            Orders.Add(order);
            return order;
        }

        internal void Refund(Order order, decimal quantity, decimal price, decimal discount, decimal tax, decimal net, bool inclusive, DateTimeOffset at)
        {
            var original = Lines.GetByOrderIdAsync(order.Id).GetAwaiter().GetResult().First();
            var refund = Clovent.Restaurant.Refunds.Refund.Create(RefundNumber.Generate(at), order.Id, Branch, order.WarehouseId, Guid.NewGuid(),
                "Synthetic cashier", "Synthetic return", Guid.NewGuid().ToString("N"), RefundSettlementMethod.CashPayout, refundedAtUtc: at);
            var line = RefundLine.Create(refund.Id, original.Id, original.ProductVariantId, "AUDIT", "Synthetic item", quantity, price, discount, tax, net,
                original.TaxClassification, original.TaxCode, original.TaxRatePercentage, inclusive, InventoryDisposition.Restock);
            refund.AddLine(line);
            var gross = net + discount - (inclusive ? 0m : tax);
            refund.FinalizeFinancials(gross, discount, tax, net, "{}");
            Refunds.AddAsync(refund).GetAwaiter().GetResult();
        }

        internal Task<Clovent.Restaurant.Application.EndOfDay.Dtos.ExpandedSalesSummaryDto> Expanded(DateOnly from, DateOnly to)
            => new GetExpandedSalesSummaryQueryHandler(Orders, Lines, Payments, Methods, Customers, Ledger,
                new FakeDiscountRepository(), new FakeServiceChargeRepository(), Shifts, new FakeTableRepository(), new FakeMediator(),
                Dates, Refunds).Handle(new(Warehouse.Value, from, to), default);
        internal Task<TaxSummaryReportDto> Tax(DateOnly from, DateOnly to)
            => new GetTaxSummaryReportQueryHandler(Orders, Lines, Refunds).Handle(new(Warehouse.Value, from, to), default);
        internal Task<CustomerReceivablesReportDto> Receivables(DateTimeOffset at)
            => new GetCustomerReceivablesReportQueryHandler(Customers, Ledger, Orders, Payments, Methods, new FakeCustomerPaymentAllocationRepository(), Scope)
                .Handle(new(at), default);
    }
}
