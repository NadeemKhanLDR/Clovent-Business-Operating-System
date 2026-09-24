using System.Reflection;
using Clovent.Catalog.Variants;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.OrderLines.Commands;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;
using System.Collections.Concurrent;
using Clovent.Restaurant.Application.SmartRecommendations.Commands;
using Clovent.Restaurant.SmartRecommendations;
using Clovent.Desktop.Sessions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.SmartPos;

public class SuggestedAddOnsPipelineTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Batch_RecordsOnlyConfirmedAcceptances_EvenWhenLaterWriteOrRefreshFails(bool failSecond)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
                var first = ProductVariantId.New();
                var second = ProductVariantId.New();
                var line = OrderLineDto.FromDomain(OrderLine.Create(order.Id, first, 1m, 47m, 0m, false));
                var mediator = new RecordingMediator(line) { FailVariant = failSecond ? second.Value : null };
                using var form = new RestaurantPosForm();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(RestaurantPosForm).GetField("_mediator", flags)!.SetValue(form, mediator);
                typeof(RestaurantPosForm).GetField("_currentOrder", flags)!.SetValue(form, OrderDto.FromDomain(order));
                typeof(RestaurantPosForm).GetField("_currentSession", flags)!.SetValue(form, new CurrentSession());
                var committed = new List<Guid>();
                var task = (Task)typeof(RestaurantPosForm).GetMethod("AddSuggestedItemsAsync", flags)!.Invoke(form,
                    new object[] { order.Id.Value, new[] { first.Value, second.Value, first.Value }, (Action<Guid>)committed.Add })!;
                // The mediator deliberately rejects refresh queries after the writes.
                Assert.ThrowsAny<Exception>(() => task.GetAwaiter().GetResult());
                var expected = failSecond ? new[] { first.Value } : new[] { first.Value, second.Value };
                Assert.Equal(expected, committed);
                Assert.True(SpinWait.SpinUntil(() => mediator.Events.Count == expected.Length, TimeSpan.FromSeconds(5)));
                Assert.Equal(expected.Order(), mediator.Events.Select(e => e.VariantId).Order());
                Assert.All(mediator.Events, e =>
                {
                    Assert.Equal(SuggestionEventKind.Accepted, e.Kind);
                    Assert.Equal(order.Id.Value, e.OrderId);
                    Assert.Equal(1m, e.AcceptedQuantity);
                    Assert.Equal(47m, e.AcceptedUnitAmount);
                    Assert.Equal(line.OrderLineId, e.OrderLineId);
                });
                Assert.Equal(2, mediator.Commands.Count(c => c is AddOrderLineCommand));
            }
            catch (Exception ex) { error = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)));
        if (error is not null) throw error;
    }

    [Theory]
    [InlineData(null, false, true)]
    [InlineData("No spice", false, false)]
    [InlineData(null, true, false)]
    public void SharedMenuAndSuggestionMutation_PreservesMergeAndVariant(string? notes, bool voided, bool shouldMerge)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var order = Order.Create(OrderType.TakeAway, WarehouseId.New());
                var variant = ProductVariantId.New();
                var line = OrderLineDto.FromDomain(OrderLine.Create(order.Id, variant, 2m, 47m, 0m, false, notes)) with { IsVoided = voided };
                var mediator = new RecordingMediator(line);
                using var form = new RestaurantPosForm();
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(RestaurantPosForm).GetField("_mediator", flags)!.SetValue(form, mediator);
                typeof(RestaurantPosForm).GetField("_currentOrder", flags)!.SetValue(form, OrderDto.FromDomain(order));
                typeof(RestaurantPosForm).GetField("_currentOrderLines", flags)!.SetValue(form, new[] { line });
                var task = (Task<OrderLineDto>)typeof(RestaurantPosForm).GetMethod("AddProductToCurrentOrderCoreAsync", flags)!
                    .Invoke(form, new object?[] { variant.Value, 1m, null })!;
                var result = task.GetAwaiter().GetResult();
                Assert.Equal(variant.Value, result.ProductVariantId);
                var command = Assert.Single(mediator.Commands);
                if (shouldMerge)
                {
                    var merge = Assert.IsType<SetOrderLineQuantityCommand>(command);
                    Assert.Equal(line.OrderLineId, merge.OrderLineId);
                    Assert.Equal(3m, merge.Quantity);
                }
                else
                {
                    var add = Assert.IsType<AddOrderLineCommand>(command);
                    Assert.Equal(order.Id.Value, add.OrderId);
                    Assert.Equal(variant.Value, add.ProductVariantId);
                    Assert.Equal(1m, add.Quantity);
                }
            }
            catch (Exception ex) { error = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)));
        if (error is not null) throw error;
    }

    private sealed class RecordingMediator(OrderLineDto line) : IMediator
    {
        public List<object> Commands { get; } = [];
        public Guid? FailVariant { get; init; }
        public ConcurrentBag<RecordSuggestionEventCommand> Events { get; } = [];
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Commands.Add(request);
            if (request is AddOrderLineCommand add)
            {
                if (add.ProductVariantId == FailVariant) throw new InvalidOperationException("Simulated write failure");
                return Task.FromResult((TResponse)(object)(line with { ProductVariantId = add.ProductVariantId }));
            }
            if (request is SetOrderLineQuantityCommand) return Task.FromResult((TResponse)(object)line);
            throw new InvalidOperationException("Simulated refresh failure");
        }
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (request is RecordSuggestionEventCommand interaction) Events.Add(interaction);
            return Task.CompletedTask;
        }
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new NotSupportedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
