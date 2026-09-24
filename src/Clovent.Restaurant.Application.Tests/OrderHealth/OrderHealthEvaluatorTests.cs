using System;
using Clovent.Restaurant.Application.OrderHealth;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.OrderHealth;

public class OrderHealthEvaluatorTests
{
    [Theory]
    [InlineData(0, OrderHealthStatus.Green)]
    [InlineData(9, OrderHealthStatus.Green)]
    [InlineData(10, OrderHealthStatus.Orange)]
    [InlineData(19, OrderHealthStatus.Orange)]
    [InlineData(20, OrderHealthStatus.Red)]
    [InlineData(120, OrderHealthStatus.Red)]
    public void DefaultThresholds_TransitionAtBoundaries(int minutes, OrderHealthStatus expected)
    {
        var result = OrderHealthEvaluator.Evaluate(TimeSpan.FromMinutes(minutes));

        Assert.Equal(expected, result.Status);
        Assert.Equal($"Waiting {minutes} min", result.DisplayText);
    }

    [Fact]
    public void OneSecondBeforeGreen_StillGreen()
    {
        var result = OrderHealthEvaluator.Evaluate(TimeSpan.FromMinutes(10) - TimeSpan.FromSeconds(1));
        Assert.Equal(OrderHealthStatus.Green, result.Status);
        Assert.Equal("Waiting 9 min", result.DisplayText);
    }

    [Fact]
    public void OneSecondPastGreen_TurnsOrange()
    {
        var result = OrderHealthEvaluator.Evaluate(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(1));
        Assert.Equal(OrderHealthStatus.Orange, result.Status);
    }

    [Fact]
    public void NegativeElapsed_TreatedAsZero()
    {
        var result = OrderHealthEvaluator.Evaluate(TimeSpan.FromMinutes(-5));
        Assert.Equal(OrderHealthStatus.Green, result.Status);
        Assert.Equal("Waiting 0 min", result.DisplayText);
    }

    [Fact]
    public void CustomThresholds_AreHonored()
    {
        var thresholds = new OrderHealthThresholds { GreenMinutes = 5, OrangeMinutes = 15 };

        Assert.Equal(OrderHealthStatus.Green, OrderHealthEvaluator.Evaluate(TimeSpan.FromMinutes(4), thresholds).Status);
        Assert.Equal(OrderHealthStatus.Orange, OrderHealthEvaluator.Evaluate(TimeSpan.FromMinutes(5), thresholds).Status);
        Assert.Equal(OrderHealthStatus.Red, OrderHealthEvaluator.Evaluate(TimeSpan.FromMinutes(15), thresholds).Status);
    }

    [Fact]
    public void InvalidThresholds_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrderHealthEvaluator.Evaluate(TimeSpan.Zero, new OrderHealthThresholds { GreenMinutes = 20, OrangeMinutes = 10 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrderHealthEvaluator.Evaluate(TimeSpan.Zero, new OrderHealthThresholds { GreenMinutes = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrderHealthEvaluator.Evaluate(TimeSpan.Zero, new OrderHealthThresholds { OrangeMinutes = -1 }));
    }
}
