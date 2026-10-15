using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Application.Refunds.Dtos;
using Clovent.Restaurant.Application.Refunds.Queries;
using Clovent.Restaurant.Refunds;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public class RefundOrderDialogTests
{
    private sealed class FakeRefundMediator : IMediator
    {
        public List<OrderLineDto> OrderLines { get; } = [];
        public List<RefundDto> ExistingRefunds { get; } = [];
        public List<ProductVariantDto> Variants { get; } = [];
        public List<ProcessRefundCommand> SentRefundCommands { get; } = [];
        public Func<ProcessRefundCommand, Task<RefundDto>>? OnProcessRefund { get; set; }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            switch (request)
            {
                case ListOrderLinesByOrderQuery:
                    return (TResponse)(object)(IReadOnlyList<OrderLineDto>)OrderLines;
                case ListRefundsByOrderQuery:
                    return (TResponse)(object)(IReadOnlyList<RefundDto>)ExistingRefunds;
                case ListProductVariantsQuery:
                    return (TResponse)(object)(IReadOnlyList<ProductVariantDto>)Variants;
                case ProcessRefundCommand cmd:
                    SentRefundCommands.Add(cmd);
                    if (OnProcessRefund != null)
                    {
                        var custom = await OnProcessRefund(cmd);
                        return (TResponse)(object)custom;
                    }

                    var dto = new RefundDto(
                        Guid.NewGuid(),
                        $"REF-20261010-{SentRefundCommands.Count:D4}",
                        cmd.OrderId,
                        Guid.NewGuid(),
                        DateTimeOffset.UtcNow,
                        cmd.CashierId,
                        cmd.CashierName,
                        cmd.Reason,
                        cmd.ApprovingUserId,
                        cmd.ApprovingUserName,
                        cmd.IdempotencyKey,
                        cmd.Items.Sum(l => l.Quantity * 500m),
                        0m,
                        0m,
                        cmd.Items.Sum(l => l.Quantity * 500m),
                        cmd.SettlementMethod.ToString(),
                        cmd.SettlementReference,
                        null,
                        null,
                        cmd.Items.Select(l => new RefundLineDto(
                            Guid.NewGuid(),
                            l.OrderLineId,
                            Guid.NewGuid(),
                            "SKU-1",
                            "Item",
                            l.Quantity,
                            500m,
                            l.Quantity * 500m,
                            0m,
                            0m,
                            l.Quantity * 500m,
                            "Taxable",
                            "PK-PRA-16",
                            16m,
                            false,
                            l.Disposition.ToString())).ToList(),
                        cmd.ShiftId,
                        cmd.TerminalId);
                    return (TResponse)(object)dto;
                default:
                    throw new NotSupportedException($"Unsupported request type: {request.GetType().Name}");
            }
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            Task.FromResult<object?>(null);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }

    private static FakeRefundMediator CreateSeededMediator(Guid orderId, out Guid line1Id, out Guid line2Id)
    {
        var mediator = new FakeRefundMediator();
        var variant1Id = Guid.NewGuid();
        var variant2Id = Guid.NewGuid();
        line1Id = Guid.NewGuid();
        line2Id = Guid.NewGuid();

        mediator.Variants.Add(new ProductVariantDto(
            variant1Id, Guid.NewGuid(), "Zinger Burger", "SKU-1", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow));
        mediator.Variants.Add(new ProductVariantDto(
            variant2Id, Guid.NewGuid(), "Loaded Fries", "SKU-2", Guid.NewGuid(), "Active", 2, DateTimeOffset.UtcNow));

        mediator.OrderLines.Add(new OrderLineDto(
            line1Id, orderId, variant1Id, 3m, 500m, 500m, false, null, null, null, 16m, false, null, false, 1500m, DateTimeOffset.UtcNow));
        mediator.OrderLines.Add(new OrderLineDto(
            line2Id, orderId, variant2Id, 2m, 300m, 300m, false, null, null, null, 16m, false, null, false, 600m, DateTimeOffset.UtcNow));

        return mediator;
    }

    [Fact]
    public async Task RefundOrderDialog_SupportsPartialAndRemainingFullRefundsWithActionBoundManagerApproval()
    {
        var orderId = Guid.NewGuid();
        var cashierId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var shiftId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();

        var mediator = CreateSeededMediator(orderId, out var line1Id, out var line2Id);

        string? capturedApprovalTitle = null;
        string? capturedApprovalDetail = null;
        string? capturedFeatureCode = null;

        using var dialog1 = new RefundOrderDialog(
            mediator,
            NullLogger.Instance,
            orderId,
            "ORD-2026-0042",
            cashierId,
            "Cashier B",
            branchId: branchId,
            shiftId: shiftId,
            terminalId: terminalId,
            authorizationPrompt: (title, detail, featureCode) =>
            {
                capturedApprovalTitle = title;
                capturedApprovalDetail = detail;
                capturedFeatureCode = featureCode;
                return Task.FromResult<ManagerAuthorizationResult?>(
                    ManagerAuthorizationResult.Approved(managerId, "Manager M"));
            });

        await dialog1.ReloadOrderLinesAsync();
        Assert.Equal(2, dialog1.LineRows.Count);
        Assert.Equal(3m, dialog1.LineRows[0].RemainingQuantity);
        Assert.Equal(2m, dialog1.LineRows[1].RemainingQuantity);

        // 1. Partial refund: 1 of 3 Zinger Burgers
        dialog1.LineRows[0].RefundQuantity = 1m;
        dialog1.ReasonEdit.Text = "Customer returned 1 burger";
        dialog1.SettlementMethodCombo.SelectedIndex = 0; // Cash Payout

        await dialog1.ExecuteSubmitRefundAsync(suppressInteractivePrompts: true);

        var cmd1 = Assert.Single(mediator.SentRefundCommands);
        Assert.Equal(orderId, cmd1.OrderId);
        Assert.Equal(shiftId, cmd1.ShiftId);
        Assert.Equal(terminalId, cmd1.TerminalId);
        Assert.Equal(managerId, cmd1.ApprovingUserId);
        Assert.Equal("Manager M", cmd1.ApprovingUserName);
        Assert.Equal("Manager Authorization - Order Refund", capturedApprovalTitle);
        Assert.Equal("pos.refund", capturedFeatureCode);
        Assert.Contains("ORD-2026-0042", capturedApprovalDetail);
        Assert.Contains("Zinger Burger x1", capturedApprovalDetail);
        Assert.Single(cmd1.Items);
        Assert.Equal(line1Id, cmd1.Items[0].OrderLineId);
        Assert.Equal(1m, cmd1.Items[0].Quantity);

        // Record the partial refund in history
        mediator.ExistingRefunds.Add(dialog1.ResultRefund!);

        // 2. Re-open dialog and click "Refund Full Remaining"
        using var dialog2 = new RefundOrderDialog(
            mediator,
            NullLogger.Instance,
            orderId,
            "ORD-2026-0042",
            cashierId,
            "Cashier B",
            branchId: branchId,
            shiftId: shiftId,
            terminalId: terminalId,
            authorizationPrompt: (_, _, _) =>
                Task.FromResult<ManagerAuthorizationResult?>(
                    ManagerAuthorizationResult.Approved(managerId, "Manager M")));

        await dialog2.ReloadOrderLinesAsync();
        Assert.Equal(2m, dialog2.LineRows[0].RemainingQuantity); // 3 - 1 = 2 remaining
        Assert.Equal(2m, dialog2.LineRows[1].RemainingQuantity); // 2 - 0 = 2 remaining

        dialog2.SelectFullRemainingQuantities();
        Assert.Equal(2m, dialog2.LineRows[0].RefundQuantity);
        Assert.Equal(2m, dialog2.LineRows[1].RefundQuantity);

        dialog2.ClearRefundQuantities();
        Assert.Equal(0m, dialog2.LineRows[0].RefundQuantity);
        Assert.Equal(0m, dialog2.LineRows[1].RefundQuantity);

        dialog2.SelectFullRemainingQuantities();
        dialog2.ReasonEdit.Text = "Refund remaining items to customer account";
        dialog2.SettlementMethodCombo.SelectedIndex = 2; // Customer Account Credit

        await dialog2.ExecuteSubmitRefundAsync(suppressInteractivePrompts: true);

        Assert.Equal(2, mediator.SentRefundCommands.Count);
        var cmd2 = mediator.SentRefundCommands[1];
        Assert.Equal(RefundSettlementMethod.CustomerAccountCredit, cmd2.SettlementMethod);
        Assert.Equal(2, cmd2.Items.Count);
        Assert.Contains(cmd2.Items, l => l.OrderLineId == line1Id && l.Quantity == 2m);
        Assert.Contains(cmd2.Items, l => l.OrderLineId == line2Id && l.Quantity == 2m);
    }

    [Fact]
    public async Task RefundOrderDialog_DeniedManagerApproval_AbortsSubmissionAndSendsNoCommand()
    {
        var orderId = Guid.NewGuid();
        var mediator = CreateSeededMediator(orderId, out _, out _);

        using var dialog = new RefundOrderDialog(
            mediator,
            NullLogger.Instance,
            orderId,
            "ORD-2026-0099",
            Guid.NewGuid(),
            "Cashier B",
            authorizationPrompt: (_, _, _) =>
                Task.FromResult<ManagerAuthorizationResult?>(
                    ManagerAuthorizationResult.Denied("Manager does not hold pos.refund")));

        await dialog.ReloadOrderLinesAsync();
        dialog.SelectFullRemainingQuantities();
        dialog.ReasonEdit.Text = "Attempted unapproved refund";

        await dialog.ExecuteSubmitRefundAsync(suppressInteractivePrompts: true);

        Assert.Empty(mediator.SentRefundCommands);
        Assert.Null(dialog.ResultRefund);
    }

    [Fact]
    public async Task RefundOrderDialog_RetryingSameSelection_ReusesStickyIdempotencyKeyUntilSelectionChanges()
    {
        var orderId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var mediator = CreateSeededMediator(orderId, out _, out _);

        int attempt = 0;
        mediator.OnProcessRefund = cmd =>
        {
            attempt++;
            if (attempt == 1)
            {
                throw new InvalidOperationException("Simulated transient failure");
            }

            return Task.FromResult(new RefundDto(
                Guid.NewGuid(),
                "REF-20261010-0001",
                cmd.OrderId,
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                cmd.CashierId,
                cmd.CashierName,
                cmd.Reason,
                cmd.ApprovingUserId,
                cmd.ApprovingUserName,
                cmd.IdempotencyKey,
                500m,
                0m,
                0m,
                500m,
                cmd.SettlementMethod.ToString(),
                null,
                null,
                null,
                []));
        };

        using var dialog = new RefundOrderDialog(
            mediator,
            NullLogger.Instance,
            orderId,
            "ORD-2026-0100",
            Guid.NewGuid(),
            "Cashier B",
            authorizationPrompt: (_, _, _) =>
                Task.FromResult<ManagerAuthorizationResult?>(
                    ManagerAuthorizationResult.Approved(managerId, "Manager M")));

        await dialog.ReloadOrderLinesAsync();
        dialog.LineRows[0].RefundQuantity = 1m;
        dialog.ReasonEdit.Text = "Retry test";

        // First submission fails transiently
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dialog.ExecuteSubmitRefundAsync(suppressInteractivePrompts: true));

        // Second submission with identical selection reuses the exact same idempotency key
        await dialog.ExecuteSubmitRefundAsync(suppressInteractivePrompts: true);

        Assert.Equal(2, mediator.SentRefundCommands.Count);
        Assert.Equal(mediator.SentRefundCommands[0].IdempotencyKey, mediator.SentRefundCommands[1].IdempotencyKey);
    }

    [Fact]
    public async Task RefundOrderDialog_AllocatedDiscountItem_PreviewsAndSubmitsNetRefundAccurately()
    {
        // Synthetic scenario: 1x White Daal Mash from ORD-1 (Ordered 2 @ 340 = 680, 10% Allocated Discount 68, Tax 0)
        var orderId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var mediator = new FakeRefundMediator();
        var variantId = Guid.NewGuid();
        var lineId = Guid.NewGuid();

        mediator.Variants.Add(new ProductVariantDto(
            variantId, Guid.NewGuid(), "White Daal Mash (Full Plate)", "WHITE-DAAL-MASH-FULL", Guid.NewGuid(), "Active", 1, DateTimeOffset.UtcNow));

        mediator.OrderLines.Add(new OrderLineDto(
            lineId, orderId, variantId, 2m, 340m, 340m, false, null, null, null,
            TaxRatePercentage: 0m,
            TaxIsInclusive: false,
            Notes: null,
            IsVoided: false,
            LineTotal: 612m,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            TaxClassification: "Exempt",
            TaxAuthority: "None",
            TaxCode: "PK-OUT-OF-SCOPE",
            TaxableBase: 612m,
            AllocatedDiscount: 68m,
            TaxAmount: 0m));

        using var dialog = new RefundOrderDialog(
            mediator,
            NullLogger.Instance,
            orderId,
            "ORD-1",
            Guid.NewGuid(),
            "System Administrator",
            authorizationPrompt: (_, _, _) =>
                Task.FromResult<ManagerAuthorizationResult?>(
                    ManagerAuthorizationResult.Approved(managerId, "System Administrator")));

        await dialog.ReloadOrderLinesAsync();
        Assert.Single(dialog.LineRows);
        var row = dialog.LineRows[0];
        Assert.Equal(2m, row.RemainingQuantity);
        Assert.Equal(0m, row.EstimatedRefund);

        // Selecting 1 unit must preview net refund Rs. 306 (340 gross - 34 allocated discount), NOT 340 gross
        row.RefundQuantity = 1m;
        Assert.Equal(306m, row.EstimatedRefund);

        // Selecting full remaining (2 units) must preview net refund Rs. 612 (680 gross - 68 allocated discount)
        dialog.SelectFullRemainingQuantities();
        Assert.Equal(612m, row.EstimatedRefund);

        // Switch back to 1 unit for partial refund
        row.RefundQuantity = 1m;
        dialog.ReasonEdit.Text = "Portion reduction";
        dialog.SettlementMethodCombo.SelectedIndex = 0; // Cash Payout

        await dialog.ExecuteSubmitRefundAsync(suppressInteractivePrompts: true);

        var cmd = Assert.Single(mediator.SentRefundCommands);
        Assert.Single(cmd.Items);
        Assert.Equal(1m, cmd.Items[0].Quantity);
        Assert.Equal(lineId, cmd.Items[0].OrderLineId);
    }
}
