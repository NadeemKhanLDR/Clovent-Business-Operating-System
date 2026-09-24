using System.Text.Json;
using Clovent.Catalog.Variants;
using Clovent.Identity.Branches;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.ActivityLogs;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.DiningAreas.ValueObjects;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Sales;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Tables;
using Clovent.Restaurant.Tables.ValueObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>
/// EF Core <see cref="ValueConverter{TModel,TProvider}"/>s shared across
/// this project's entity type configurations - see
/// <c>Clovent.Catalog.Infrastructure.Persistence.ValueConverters</c> for the
/// identical pattern and reasoning.
/// </summary>
internal static class ValueConverters
{
    /// <summary><see cref="DiningAreaId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<DiningAreaId, Guid> DiningAreaIdConverter =
        new(id => id.Value, value => new DiningAreaId(value));

    /// <summary><see cref="TableId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<TableId, Guid> TableIdConverter =
        new(id => id.Value, value => new TableId(value));

    /// <summary>Nullable <see cref="TableId"/> &lt;-&gt; nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<TableId?, Guid?> NullableTableIdConverter =
        new(id => id == null ? null : id.Value.Value, value => value == null ? null : new TableId(value.Value));

    /// <summary><see cref="OrderId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<OrderId, Guid> OrderIdConverter =
        new(id => id.Value, value => new OrderId(value));

    /// <summary><see cref="OrderLineId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<OrderLineId, Guid> OrderLineIdConverter =
        new(id => id.Value, value => new OrderLineId(value));

    /// <summary><see cref="KitchenTicketId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<KitchenTicketId, Guid> KitchenTicketIdConverter =
        new(id => id.Value, value => new KitchenTicketId(value));

    /// <summary><see cref="PaymentId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<PaymentId, Guid> PaymentIdConverter =
        new(id => id.Value, value => new PaymentId(value));

    /// <summary><see cref="PaymentMethodId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<PaymentMethodId, Guid> PaymentMethodIdConverter =
        new(id => id.Value, value => new PaymentMethodId(value));

    /// <summary><see cref="DiscountId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<DiscountId, Guid> DiscountIdConverter =
        new(id => id.Value, value => new DiscountId(value));

    /// <summary><see cref="ServiceChargeId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ServiceChargeId, Guid> ServiceChargeIdConverter =
        new(id => id.Value, value => new ServiceChargeId(value));

    /// <summary><see cref="BranchId"/> (from <c>Clovent.Identity</c>) &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<BranchId, Guid> BranchIdConverter =
        new(id => id.Value, value => new BranchId(value));

    /// <summary><see cref="WarehouseId"/> (from <c>Clovent.MasterData</c>) &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<WarehouseId, Guid> WarehouseIdConverter =
        new(id => id.Value, value => new WarehouseId(value));

    /// <summary><see cref="ProductVariantId"/> (from <c>Clovent.Catalog</c>) &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ProductVariantId, Guid> ProductVariantIdConverter =
        new(id => id.Value, value => new ProductVariantId(value));

    /// <summary><see cref="EntityCode"/> (from <c>Clovent.MasterData</c>, reused for <see cref="Table.Code"/>) &lt;-&gt; code text.</summary>
    public static readonly ValueConverter<EntityCode, string> EntityCodeConverter =
        new(v => v.Value, v => EntityCode.Create(v));

    /// <summary><see cref="TableName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<TableName, string> TableNameConverter =
        new(v => v.Value, v => TableName.Create(v));

    /// <summary><see cref="DiningAreaName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<DiningAreaName, string> DiningAreaNameConverter =
        new(v => v.Value, v => DiningAreaName.Create(v));

    /// <summary><see cref="PaymentMethodName"/> &lt;-&gt; name text.</summary>
    public static readonly ValueConverter<PaymentMethodName, string> PaymentMethodNameConverter =
        new(v => v.Value, v => PaymentMethodName.Create(v));

    /// <summary><see cref="OrderNumber"/> &lt;-&gt; display text.</summary>
    public static readonly ValueConverter<OrderNumber, string> OrderNumberConverter =
        new(v => v.Value, v => OrderNumber.Create(v));

    /// <summary><see cref="DailySalesSequenceId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<DailySalesSequenceId, Guid> DailySalesSequenceIdConverter =
        new(id => id.Value, value => new DailySalesSequenceId(value));

    /// <summary><see cref="OrderNumberSequenceId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<OrderNumberSequenceId, Guid> OrderNumberSequenceIdConverter =
        new(id => id.Value, value => new OrderNumberSequenceId(value));

    /// <summary><see cref="ActivityLogEntryId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<ActivityLogEntryId, Guid> ActivityLogEntryIdConverter =
        new(id => id.Value, value => new ActivityLogEntryId(value));

    /// <summary><see cref="Order.OrderLineIds"/> &lt;-&gt; a JSON array of order line id GUIDs - identical reasoning to <c>Clovent.Identity.Infrastructure.Persistence.ValueConverters.CompanyIdsConverter</c>.</summary>
    public static readonly ValueConverter<IReadOnlyCollection<OrderLineId>, string> OrderLineIdsConverter = new(
        v => JsonSerializer.Serialize(v.Select(i => i.Value), (JsonSerializerOptions?)null),
        v => (JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()).Select(g => new OrderLineId(g)).ToList());

    /// <summary><see cref="Order.OrderLineIds"/>'s change-tracking comparer - identical reasoning to <c>CompanyIdsComparer</c>.</summary>
    public static readonly ValueComparer<IReadOnlyCollection<OrderLineId>> OrderLineIdsComparer = new(
        (a, b) => (a ?? new List<OrderLineId>()).OrderBy(i => i.Value).SequenceEqual((b ?? new List<OrderLineId>()).OrderBy(i => i.Value)),
        v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
        v => v.ToList());

    /// <summary><see cref="Order.DiscountIds"/> &lt;-&gt; a JSON array of discount id GUIDs.</summary>
    public static readonly ValueConverter<IReadOnlyCollection<DiscountId>, string> DiscountIdsConverter = new(
        v => JsonSerializer.Serialize(v.Select(i => i.Value), (JsonSerializerOptions?)null),
        v => (JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()).Select(g => new DiscountId(g)).ToList());

    /// <summary><see cref="Order.DiscountIds"/>'s change-tracking comparer.</summary>
    public static readonly ValueComparer<IReadOnlyCollection<DiscountId>> DiscountIdsComparer = new(
        (a, b) => (a ?? new List<DiscountId>()).OrderBy(i => i.Value).SequenceEqual((b ?? new List<DiscountId>()).OrderBy(i => i.Value)),
        v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
        v => v.ToList());

    /// <summary><see cref="Order.ServiceChargeIds"/> &lt;-&gt; a JSON array of service charge id GUIDs.</summary>
    public static readonly ValueConverter<IReadOnlyCollection<ServiceChargeId>, string> ServiceChargeIdsConverter = new(
        v => JsonSerializer.Serialize(v.Select(i => i.Value), (JsonSerializerOptions?)null),
        v => (JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()).Select(g => new ServiceChargeId(g)).ToList());

    /// <summary><see cref="Order.ServiceChargeIds"/>'s change-tracking comparer.</summary>
    public static readonly ValueComparer<IReadOnlyCollection<ServiceChargeId>> ServiceChargeIdsComparer = new(
        (a, b) => (a ?? new List<ServiceChargeId>()).OrderBy(i => i.Value).SequenceEqual((b ?? new List<ServiceChargeId>()).OrderBy(i => i.Value)),
        v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
        v => v.ToList());

    /// <summary><see cref="Order.PaymentIds"/> &lt;-&gt; a JSON array of payment id GUIDs.</summary>
    public static readonly ValueConverter<IReadOnlyCollection<PaymentId>, string> PaymentIdsConverter = new(
        v => JsonSerializer.Serialize(v.Select(i => i.Value), (JsonSerializerOptions?)null),
        v => (JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()).Select(g => new PaymentId(g)).ToList());

    /// <summary><see cref="Order.PaymentIds"/>'s change-tracking comparer.</summary>
    public static readonly ValueComparer<IReadOnlyCollection<PaymentId>> PaymentIdsComparer = new(
        (a, b) => (a ?? new List<PaymentId>()).OrderBy(i => i.Value).SequenceEqual((b ?? new List<PaymentId>()).OrderBy(i => i.Value)),
        v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
        v => v.ToList());

    /// <summary><see cref="KitchenTicket.OrderLineIds"/> &lt;-&gt; a JSON array of order line id GUIDs (the send-time snapshot - see <see cref="KitchenTicket"/>'s doc comment).</summary>
    public static readonly ValueConverter<IReadOnlyCollection<OrderLineId>, string> KitchenTicketOrderLineIdsConverter = new(
        v => JsonSerializer.Serialize(v.Select(i => i.Value), (JsonSerializerOptions?)null),
        v => (JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()).Select(g => new OrderLineId(g)).ToList());

    /// <summary><see cref="KitchenTicket.OrderLineIds"/>'s change-tracking comparer.</summary>
    public static readonly ValueComparer<IReadOnlyCollection<OrderLineId>> KitchenTicketOrderLineIdsComparer = new(
        (a, b) => (a ?? new List<OrderLineId>()).OrderBy(i => i.Value).SequenceEqual((b ?? new List<OrderLineId>()).OrderBy(i => i.Value)),
        v => v.Aggregate(0, (hash, id) => HashCode.Combine(hash, id)),
        v => v.ToList());

    /// <summary><see cref="CustomerId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<CustomerId, Guid> CustomerIdConverter =
        new(id => id.Value, value => new CustomerId(value));

    /// <summary>Nullable <see cref="CustomerId"/> &lt;-&gt; nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<CustomerId?, Guid?> NullableCustomerIdConverter =
        new(id => id == null ? null : id.Value.Value, value => value == null ? null : new CustomerId(value.Value));

    /// <summary><see cref="CustomerLedgerEntryId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<CustomerLedgerEntryId, Guid> CustomerLedgerEntryIdConverter =
        new(id => id.Value, value => new CustomerLedgerEntryId(value));

    /// <summary><see cref="Clovent.Restaurant.Shifts.ShiftId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.Restaurant.Shifts.ShiftId, Guid> ShiftIdConverter =
        new(id => id.Value, value => new Clovent.Restaurant.Shifts.ShiftId(value));

    /// <summary>Nullable <see cref="Clovent.Restaurant.Shifts.ShiftId"/> &lt;-&gt; nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.Restaurant.Shifts.ShiftId?, Guid?> NullableShiftIdConverter =
        new(id => id == null ? null : id.Value.Value, value => value == null ? null : new Clovent.Restaurant.Shifts.ShiftId(value.Value));

    /// <summary><see cref="Clovent.Restaurant.Shifts.CashMovementId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.Restaurant.Shifts.CashMovementId, Guid> CashMovementIdConverter =
        new(id => id.Value, value => new Clovent.Restaurant.Shifts.CashMovementId(value));

    /// <summary><see cref="Clovent.MasterData.Terminals.TerminalId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.MasterData.Terminals.TerminalId, Guid> TerminalIdConverter =
        new(id => id.Value, value => new Clovent.MasterData.Terminals.TerminalId(value));

    /// <summary><see cref="Clovent.Identity.Users.UserId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.Identity.Users.UserId, Guid> UserIdConverter =
        new(id => id.Value, value => new Clovent.Identity.Users.UserId(value));

    /// <summary><see cref="Clovent.Restaurant.SmartRecommendations.RecommendationRuleId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.Restaurant.SmartRecommendations.RecommendationRuleId, Guid> RecommendationRuleIdConverter =
        new(id => id.Value, value => new Clovent.Restaurant.SmartRecommendations.RecommendationRuleId(value));

    /// <summary><see cref="Clovent.Restaurant.SmartRecommendations.SuggestionEventId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.Restaurant.SmartRecommendations.SuggestionEventId, Guid> SuggestionEventIdConverter =
        new(id => id.Value, value => new Clovent.Restaurant.SmartRecommendations.SuggestionEventId(value));

    /// <summary><see cref="Clovent.Restaurant.QuickOrderTemplates.QuickOrderTemplateId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.Restaurant.QuickOrderTemplates.QuickOrderTemplateId, Guid> QuickOrderTemplateIdConverter =
        new(id => id.Value, value => new Clovent.Restaurant.QuickOrderTemplates.QuickOrderTemplateId(value));

    /// <summary><see cref="Clovent.Restaurant.QuickOrderTemplates.QuickOrderTemplateItemId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.Restaurant.QuickOrderTemplates.QuickOrderTemplateItemId, Guid> QuickOrderTemplateItemIdConverter =
        new(id => id.Value, value => new Clovent.Restaurant.QuickOrderTemplates.QuickOrderTemplateItemId(value));

    /// <summary><see cref="Clovent.Restaurant.DayClose.BusinessDayCloseId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.Restaurant.DayClose.BusinessDayCloseId, Guid> BusinessDayCloseIdConverter =
        new(id => id.Value, value => new Clovent.Restaurant.DayClose.BusinessDayCloseId(value));

    /// <summary><see cref="Clovent.Restaurant.Attendance.AttendanceSessionId"/> &lt;-&gt; <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.Restaurant.Attendance.AttendanceSessionId, Guid> AttendanceSessionIdConverter =
        new(id => id.Value, value => new Clovent.Restaurant.Attendance.AttendanceSessionId(value));

    /// <summary>Nullable <see cref="Clovent.MasterData.Terminals.TerminalId"/> &lt;-&gt; nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<Clovent.MasterData.Terminals.TerminalId?, Guid?> NullableTerminalIdConverter =
        new(id => id.HasValue ? id.Value.Value : null, value => value.HasValue ? new Clovent.MasterData.Terminals.TerminalId(value.Value) : null);
}

