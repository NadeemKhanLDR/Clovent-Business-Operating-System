using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Application.CashierAudits.Commands;
using Clovent.Restaurant.Application.CashierAudits.Services;
using Clovent.Restaurant.Application.Outbox.Dtos;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.AuditAlerts;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.Outbox;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Shifts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.CashierAudits;

public sealed class CashierAuditAnalyzerServiceTests
{
    private readonly FakeShiftRepository _shiftRepo = new();
    private readonly FakeOrderRepository _orderRepo = new();
    private readonly FakeOrderLineRepository _orderLineRepo = new();
    private readonly FakePaymentRepository _paymentRepo = new();
    private readonly FakePaymentMethodRepository _paymentMethodRepo = new();
    private readonly FakeDiscountRepository _discountRepo = new();
    private readonly FakeActivityLogEntryRepository _activityLogRepo = new();
    private readonly FakeCashierAuditAlertRepository _alertRepo = new();
    private readonly FakeOutboxRepository _outboxRepo = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly CashierAuditAnalyzerService _service;

    private readonly BranchId _branchId = BranchId.New();
    private readonly WarehouseId _warehouseId = WarehouseId.New();
    private readonly TerminalId _terminalId = TerminalId.New();
    private readonly UserId _cashierId = UserId.New();
    private const string CashierName = "Zaid Ahmed";
    private readonly PaymentMethod _cashMethod;

    public CashierAuditAnalyzerServiceTests()
    {
        _cashMethod = PaymentMethod.Create(PaymentMethodName.Create("Cash"));
        _paymentMethodRepo.AddAsync(_cashMethod).GetAwaiter().GetResult();

        _service = new CashierAuditAnalyzerService(
            _shiftRepo,
            _orderRepo,
            _orderLineRepo,
            _paymentRepo,
            _paymentMethodRepo,
            _discountRepo,
            _activityLogRepo,
            _alertRepo,
            _outboxRepo,
            _unitOfWork,
            NullLogger<CashierAuditAnalyzerService>.Instance);
    }

    [Fact]
    public async Task RunAnalysis_WhenOrderVoidedAfterCashTender_SurfacesCriticalAlertAndOutboxNotification()
    {
        // Arrange
        var shift = Shift.Open(101, _branchId, _warehouseId, _terminalId, _cashierId, CashierName, 5000m);
        await _shiftRepo.AddAsync(shift);

        var order = Order.Create(OrderType.TakeAway, _warehouseId, null, OrderNumber.Generate(DateTimeOffset.UtcNow));
        await _orderRepo.AddAsync(order);

        // Add line
        var line = OrderLine.Create(order.Id, ProductVariantId.New(), 2, 750m, 0m, false, null);
        order.AddOrderLine(line.Id);
        await _orderLineRepo.AddAsync(line);

        // Record cash payment
        var payment = Payment.Create(order.Id, _cashMethod.Id, 1500m, shift.Id);
        order.RecordPayment(payment.Id);
        await _paymentRepo.AddAsync(payment);

        // Cashier voids the order after receiving cash
        order.Void("Customer cancelled order");

        // Act
        var result = await _service.RunAnalysisAsync(
            fromUtc: DateTimeOffset.UtcNow.AddHours(-1),
            toUtc: DateTimeOffset.UtcNow.AddHours(1),
            specificShiftId: shift.Id);

        // Assert
        Assert.True(result.NewAlertsGenerated >= 1);
        var alert = Assert.Single(result.Alerts, a => a.AnomalyType == nameof(CashierAnomalyType.ExcessiveVoidsAfterCashTender));
        Assert.Equal(nameof(AuditAlertSeverity.Critical), alert.Severity);
        Assert.True(alert.RiskScore >= 90.0m);
        Assert.Equal(CashierName, alert.CashierName);
        Assert.Contains(order.OrderNumber.Value, alert.Description);

        // Outbox notification verification
        var outboxMessage = Assert.Single(_outboxRepo.AllMessages, m => m.MessageType == OutboxMessageType.CashierAuditAlert);
        Assert.Equal("CashierAuditAlert", outboxMessage.AggregateType);
        var payload = JsonSerializer.Deserialize<CashierAuditAlertOutboxPayload>(outboxMessage.Payload);
        Assert.NotNull(payload);
        Assert.Equal(CashierName, payload.CashierName);
        Assert.Equal(nameof(AuditAlertSeverity.Critical), payload.Severity);
    }

    [Fact]
    public async Task RunAnalysis_WhenLinesVoidedAfterCashTender_SurfacesHighAlert()
    {
        // Arrange
        var shift = Shift.Open(102, _branchId, _warehouseId, _terminalId, _cashierId, CashierName, 5000m);
        await _shiftRepo.AddAsync(shift);

        var order = Order.Create(OrderType.TakeAway, _warehouseId, null, OrderNumber.Generate(DateTimeOffset.UtcNow));
        await _orderRepo.AddAsync(order);

        var line1 = OrderLine.Create(order.Id, ProductVariantId.New(), 1, 800m, 0m, false, null);
        var line2 = OrderLine.Create(order.Id, ProductVariantId.New(), 1, 600m, 0m, false, null);
        order.AddOrderLine(line1.Id);
        order.AddOrderLine(line2.Id);
        await _orderLineRepo.AddAsync(line1);
        await _orderLineRepo.AddAsync(line2);

        var payment = Payment.Create(order.Id, _cashMethod.Id, 1400m, shift.Id);
        order.RecordPayment(payment.Id);
        await _paymentRepo.AddAsync(payment);

        // Cashier voids line1 (worth Rs. 800 >= 500 threshold)
        line1.Void();

        // Act
        var result = await _service.RunAnalysisAsync(specificShiftId: shift.Id);

        // Assert
        var alert = Assert.Single(result.Alerts, a => a.AnomalyType == nameof(CashierAnomalyType.ExcessiveVoidsAfterCashTender));
        Assert.Equal(nameof(AuditAlertSeverity.High), alert.Severity);
        Assert.True(alert.RiskScore >= 70.0m);
        Assert.Contains("Rs. 800.00", alert.Description);
    }

    [Fact]
    public async Task RunAnalysis_WhenCashierHasFrequentManagerOverrides_SurfacesAnomalyAlert()
    {
        // Arrange
        var shift = Shift.Open(103, _branchId, _warehouseId, _terminalId, _cashierId, CashierName, 5000m);
        await _shiftRepo.AddAsync(shift);

        // Seed 4 manager overrides for this cashier during the shift
        for (int i = 1; i <= 4; i++)
        {
            var log = ActivityLogEntry.Record(
                "Price Override",
                $"Price overridden on item. Approved by manager 'Store Manager'.",
                CashierName,
                "POS-01");
            await _activityLogRepo.AddAsync(log);
        }

        // Act
        var result = await _service.RunAnalysisAsync(specificShiftId: shift.Id);

        // Assert
        var alert = Assert.Single(result.Alerts, a => a.AnomalyType == nameof(CashierAnomalyType.FrequentManagerOverrides));
        Assert.Equal(CashierName, alert.CashierName);
        Assert.True(alert.RiskScore >= 50.0m);
        Assert.Contains("4 manager overrides", alert.Description);
    }

    [Fact]
    public async Task RunAnalysis_WhenUnlinkedCashDrawerOpeningsOccur_SurfacesAnomalyAlert()
    {
        // Arrange
        var shift = Shift.Open(104, _branchId, _warehouseId, _terminalId, _cashierId, CashierName, 5000m);
        await _shiftRepo.AddAsync(shift);

        // Record 3 unlinked drawer kick events
        for (int i = 1; i <= 3; i++)
        {
            var log = ActivityLogEntry.Record("Open Drawer", "Cash Drawer Kick - No sale", CashierName, "POS-01");
            await _activityLogRepo.AddAsync(log);
        }

        // Act
        var result = await _service.RunAnalysisAsync(specificShiftId: shift.Id);

        // Assert
        var alert = Assert.Single(result.Alerts, a => a.AnomalyType == nameof(CashierAnomalyType.UnlinkedCashDrawerOpening));
        Assert.Equal(CashierName, alert.CashierName);
        Assert.Contains("3 unlinked cash drawer openings", alert.Description);
    }

    [Fact]
    public async Task RunAnalysis_WhenUnusualDiscountSpikeOrClusterOccurs_SurfacesAlert()
    {
        // Arrange
        var shift = Shift.Open(105, _branchId, _warehouseId, _terminalId, _cashierId, CashierName, 5000m);
        await _shiftRepo.AddAsync(shift);

        var order = Order.Create(OrderType.TakeAway, _warehouseId, null, OrderNumber.Generate(DateTimeOffset.UtcNow));
        await _orderRepo.AddAsync(order);

        // High 30% discount
        var discount = Discount.Create(order.Id, DiscountType.Percentage, 30m, "Special Manager Courtesy");
        order.ApplyDiscount(discount.Id);
        await _discountRepo.AddAsync(discount);

        // Act
        var result = await _service.RunAnalysisAsync(specificShiftId: shift.Id);

        // Assert
        var alert = Assert.Single(result.Alerts, a => a.AnomalyType == nameof(CashierAnomalyType.UnusualDiscountCluster));
        Assert.Equal(CashierName, alert.CashierName);
        Assert.Contains("30%", alert.Description);
    }

    [Fact]
    public async Task ComputeCashierRiskScores_CalculatesRiskLevelsAndSparklineTrends()
    {
        // Arrange
        var shift1 = Shift.Open(106, _branchId, _warehouseId, _terminalId, _cashierId, "HighRiskCashier", 5000m);
        await _shiftRepo.AddAsync(shift1);

        var alert1 = CashierAuditAlert.Create(
            shift1.Id, null, _cashierId, "HighRiskCashier",
            CashierAnomalyType.ExcessiveVoidsAfterCashTender,
            AuditAlertSeverity.Critical, 90.0m, "Void after cash");
        var alert2 = CashierAuditAlert.Create(
            shift1.Id, null, _cashierId, "HighRiskCashier",
            CashierAnomalyType.FrequentManagerOverrides,
            AuditAlertSeverity.High, 75.0m, "Overrides");
        await _alertRepo.AddAsync(alert1);
        await _alertRepo.AddAsync(alert2);

        // Act
        var scores = await _service.ComputeCashierRiskScoresAsync();

        // Assert
        var cashierProfile = Assert.Single(scores, s => s.CashierName == "HighRiskCashier");
        Assert.True(cashierProfile.OverallRiskScore >= 75.0m);
        Assert.Equal("Critical", cashierProfile.RiskLevel);
        Assert.Equal(2, cashierProfile.ActiveAlertsCount);
        Assert.Equal(7, cashierProfile.RiskTrendSparkline.Count);
    }

    [Fact]
    public async Task ReviewCommandHandler_WhenReviewed_UpdatesStatusAndNotes()
    {
        // Arrange
        var alert = CashierAuditAlert.Create(
            null, null, null, "TestCashier",
            CashierAnomalyType.UnusualDiscountCluster,
            AuditAlertSeverity.Medium, 55m, "Test description");
        await _alertRepo.AddAsync(alert);

        var handler = new ReviewCashierAuditAlertCommandHandler(_alertRepo, _unitOfWork);

        // Act
        var result = await handler.Handle(
            new ReviewCashierAuditAlertCommand(alert.Id.Value, "Manager Asad", "Verified staff discount voucher"),
            CancellationToken.None);

        // Assert
        Assert.Equal(nameof(AuditAlertStatus.Reviewed), result.Status);
        Assert.Equal("Manager Asad", result.ReviewedBy);
        Assert.Equal("Verified staff discount voucher", result.ResolutionNotes);
    }
}
