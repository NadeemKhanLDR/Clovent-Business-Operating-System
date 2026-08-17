using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Verifies the POS product-tile host reflows columns purely from available width,
/// mirroring RestaurantPosForm's _productTilesFlow configuration (TileWidth/TileHeight
/// constants, tile Margin, FlowLayoutPanel Padding, default WrapContents=true, AutoScroll=true).
/// </summary>
public class ProductTileWrappingTests
{
    private const int TileWidth = 160;
    private const int TileHeight = 150;
    private const int TileMargin = 5;
    private const int FlowPadding = 10;

    private static FlowLayoutPanel CreateTileHost(int tileCount)
    {
        var flow = new FlowLayoutPanel
        {
            AutoScroll = true,
            Padding = new Padding(FlowPadding),
        };

        for (var i = 0; i < tileCount; i++)
        {
            flow.Controls.Add(new Panel
            {
                Size = new Size(TileWidth, TileHeight),
                Margin = new Padding(TileMargin),
            });
        }

        return flow;
    }

    private static int CountTilesInFirstRow(FlowLayoutPanel flow)
    {
        flow.PerformLayout();
        var firstRowTop = flow.Controls[0].Top;
        return flow.Controls.Cast<Control>().Count(c => c.Top == firstRowTop);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void TileHost_WrapsToExactColumnCountAvailableWidthAllows(int expectedColumns)
    {
        var tileFootprint = TileWidth + (TileMargin * 2);
        var width = (FlowPadding * 2) + (tileFootprint * expectedColumns) + (tileFootprint / 2);

        var flow = CreateTileHost(tileCount: expectedColumns * 3);
        flow.Width = width;
        flow.Height = 900;

        var actualColumns = CountTilesInFirstRow(flow);

        Assert.Equal(expectedColumns, actualColumns);
    }

    [Fact]
    public void TileHost_DefaultWrapContentsIsTrue_TileWidthIndependentOfPanelWidth()
    {
        using var flow = CreateTileHost(tileCount: 6);

        Assert.True(flow.WrapContents);
        Assert.True(flow.AutoScroll);
        Assert.Equal(FlowDirection.LeftToRight, flow.FlowDirection);

        flow.Width = 2000;
        flow.PerformLayout();

        Assert.All(flow.Controls.Cast<Control>(), c => Assert.Equal(TileWidth, c.Width));
    }
}
