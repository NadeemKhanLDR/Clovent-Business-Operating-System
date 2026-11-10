using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

public sealed class PaymentHistoryRulesTests
{
    [Fact]
    public void EvaluateVoidAvailability_CompletedOrder_DisablesVoidWithRefundWorkflowMessage()
    {
        var (canVoid, explanation) = PaymentHistoryRules.EvaluateVoidAvailability(OrderStatus.Completed);

        Assert.False(canVoid);
        Assert.False(string.IsNullOrWhiteSpace(explanation));
        Assert.Contains("Refund Order", explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OrderStatus.Open)]
    [InlineData(OrderStatus.Held)]
    public void EvaluateVoidAvailability_OpenOrHeldOrder_AllowsVoidWithoutBlockingBanner(OrderStatus status)
    {
        var (canVoid, explanation) = PaymentHistoryRules.EvaluateVoidAvailability(status);

        Assert.True(canVoid);
        Assert.Null(explanation);
    }
}
