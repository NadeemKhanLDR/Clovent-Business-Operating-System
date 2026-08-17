using Clovent.Catalog.Application.Categories.Dtos;
using Clovent.Catalog.Application.Variants.Dtos;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Fake data for the design-time-only constructors of <see cref="RestaurantPosForm"/>
/// (see the form's own <c>InitializeDesignTime</c>).
/// Never touches DI/MediatR/SQL/configuration/session - the Visual Studio
/// Designer host runs this in-process while editing the screen, so nothing
/// here may depend on anything not resolvable purely by calling <see langword="new"/>.
/// Exists only so the designer canvas has something realistic to render;
/// none of this is used once the screen actually runs.
/// </summary>
internal static class RestaurantPosDesignDataProvider
{
    public sealed record CartLine(
        Guid OrderLineId,
        string Sku,
        string Name,
        decimal Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        string Notes,
        bool IsVoided,
        bool IsPriceOverridden);

    public sealed record Totals(
        decimal Subtotal,
        decimal Discount,
        decimal Tax,
        decimal ServiceCharge,
        decimal GrandTotal,
        decimal Paid,
        decimal Balance);

    public const string CashierName = "Sample Cashier";
    public const string CustomerName = "Walk-in Customer";
    public const string TableCode = "Table 3";

    public static IReadOnlyList<(Guid Id, string Display)> Warehouses { get; } =
    [
        (Guid.NewGuid(), "Main Location"),
        (Guid.NewGuid(), "Express Counter"),
    ];

    public static IReadOnlyList<(Guid Id, string Display)> Tables { get; } =
    [
        (Guid.NewGuid(), "Table 1"),
        (Guid.NewGuid(), "Table 2"),
        (Guid.NewGuid(), TableCode),
    ];

    public static IReadOnlyList<ProductCategoryDto> Categories { get; }

    public static IReadOnlyList<ProductVariantDto> Variants { get; }

    public static IReadOnlyDictionary<Guid, decimal> SellingPricesByVariantId { get; }

    public static IReadOnlyList<CartLine> CartLines { get; }

    public static Totals SampleTotals { get; } = new(
        Subtotal: 50.00m,
        Discount: 0.00m,
        Tax: 0.00m,
        ServiceCharge: 0.00m,
        GrandTotal: 50.00m,
        Paid: 0.00m,
        Balance: 50.00m);

    /// <summary>Names only - the form's <c>InitializeDesignTime</c> assigns each its own <see cref="Guid"/>.</summary>
    public static IReadOnlyList<string> PaymentMethodNames { get; } =
    [
        "Cash",
        "Credit Card",
        "EasyPaisa",
        "Visa",
        "MasterCard",
    ];

    static RestaurantPosDesignDataProvider()
    {
        var beverages = Guid.NewGuid();
        var bread = Guid.NewGuid();
        var snacks = Guid.NewGuid();

        Categories =
        [
            new ProductCategoryDto(beverages, "Beverages", null, "Active", "#3B82F6", 1, DateTimeOffset.UtcNow),
            new ProductCategoryDto(bread, "Bread", null, "Active", "#10B981", 2, DateTimeOffset.UtcNow),
            new ProductCategoryDto(snacks, "Snacks", null, "Active", "#F59E0B", 3, DateTimeOffset.UtcNow),
        ];

        var garlicNan = Guid.NewGuid();
        var leechi = Guid.NewGuid();
        var coffee = Guid.NewGuid();
        var burger = Guid.NewGuid();

        Variants =
        [
            new ProductVariantDto(garlicNan, Guid.NewGuid(), "Garlic Nan", "GARLICNAN", Guid.Empty, "Active", 1, DateTimeOffset.UtcNow, bread),
            new ProductVariantDto(leechi, Guid.NewGuid(), "Leechi", "LEECHI", Guid.Empty, "Active", 2, DateTimeOffset.UtcNow, beverages),
            new ProductVariantDto(coffee, Guid.NewGuid(), "Coffee", "COFFEE", Guid.Empty, "Active", 3, DateTimeOffset.UtcNow, beverages),
            new ProductVariantDto(burger, Guid.NewGuid(), "Burger", "BURGER", Guid.Empty, "Active", 4, DateTimeOffset.UtcNow, snacks),
        ];

        SellingPricesByVariantId = new Dictionary<Guid, decimal>
        {
            [garlicNan] = 1.50m,
            [leechi] = 2.50m,
            [coffee] = 10.00m,
            [burger] = 20.00m,
        };

        CartLines =
        [
            new CartLine(Guid.NewGuid(), "COFFEE", "Coffee", 1, 10.00m, 10.00m, "Sugar-free", false, false),
            new CartLine(Guid.NewGuid(), "BURGER", "Burger", 2, 20.00m, 40.00m, "", false, false),
        ];
    }
}
