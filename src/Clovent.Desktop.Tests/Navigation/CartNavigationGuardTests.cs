using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Shell;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;
using Xunit;

namespace Clovent.Desktop.Tests.Navigation;

public class CartNavigationGuardTests
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    private sealed class FakeModeNavigator : IApplicationModeNavigator
    {
        public int OpenBackOfficeCallCount { get; private set; }
        public int OpenPosCallCount { get; private set; }
        public Form? CurrentForm => null;
        public IWorkspaceHost? CurrentWorkspaceHost => null;
        public bool IsTransitioning => false;
        public ApplicationContext ApplicationContext { get; } = new CbosApplicationContext();

        public Task OpenPosAsync(Clovent.Restaurant.Application.Shifts.Dtos.ShiftDto? activeShift = null) => Task.CompletedTask;

        public Task OpenBackOfficeAsync(string? initialViewKey = "dashboard", string? initialCaption = "Dashboard")
        {
            OpenBackOfficeCallCount++;
            return Task.CompletedTask;
        }

        public void ExitApplication() { }
        public void ExitApplication(string initiator) { }
    }

    private static OrderDto CreateOrderDto(string status, int lineCount)
    {
        var lineIds = Enumerable.Range(0, lineCount).Select(_ => Guid.NewGuid()).ToList();
        return new OrderDto(
            OrderId: Guid.NewGuid(),
            OrderNumber: "ORD-001",
            DailySalesNumber: 1,
            OrderType: "TakeAway",
            Status: status,
            TableId: null,
            WarehouseId: Guid.NewGuid(),
            Notes: null,
            CustomerNotes: null,
            OrderLineIds: lineIds,
            DiscountIds: Array.Empty<Guid>(),
            ServiceChargeIds: Array.Empty<Guid>(),
            PaymentIds: Array.Empty<Guid>(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            CustomerId: null);
    }

    private sealed class FakeMediator : IMediator
    {
        public int HoldOrderCallCount { get; private set; }
        public bool ThrowOnHold { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is HoldOrderCommand)
            {
                HoldOrderCallCount++;
                if (ThrowOnHold)
                {
                    throw new InvalidOperationException("Simulated hold order failure in database");
                }

                var order = CreateOrderDto("Held", 1);
                return Task.FromResult((TResponse)(object)order);
            }

            throw new NotImplementedException($"Unhandled request: {request.GetType().Name}");
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    [Fact]
    public void HasInProgressOrder_ReturnsFalse_WhenNoOrderSelected()
    {
        using var form = new RestaurantPosForm();
        typeof(RestaurantPosForm).GetField("_currentOrder", Flags)!.SetValue(form, null);

        Assert.False(form.HasInProgressOrder());
    }

    [Fact]
    public void HasInProgressOrder_ReturnsFalse_WhenOrderHasNoLines()
    {
        using var form = new RestaurantPosForm();
        var order = CreateOrderDto("Open", 0);
        typeof(RestaurantPosForm).GetField("_currentOrder", Flags)!.SetValue(form, order);

        Assert.False(form.HasInProgressOrder());
    }

    [Fact]
    public void HasInProgressOrder_ReturnsFalse_WhenOrderIsNotOpen()
    {
        using var form = new RestaurantPosForm();
        var order = CreateOrderDto("Held", 2);
        typeof(RestaurantPosForm).GetField("_currentOrder", Flags)!.SetValue(form, order);

        Assert.False(form.HasInProgressOrder());
    }

    [Fact]
    public void HasInProgressOrder_ReturnsTrue_WhenOrderIsOpen_AndHasLines()
    {
        using var form = new RestaurantPosForm();
        var order = CreateOrderDto("Open", 2);
        typeof(RestaurantPosForm).GetField("_currentOrder", Flags)!.SetValue(form, order);

        Assert.True(form.HasInProgressOrder());
    }

    [Fact]
    public async Task RequestNavigateToBackOfficeAsync_DirectTransition_WhenCartEmpty()
    {
        using var form = new RestaurantPosForm();
        var fakeNavigator = new FakeModeNavigator();
        form.SetApplicationModeNavigator(fakeNavigator);

        // Cart is empty
        typeof(RestaurantPosForm).GetField("_currentOrder", Flags)!.SetValue(form, null);

        await form.RequestNavigateToBackOfficeAsync();

        Assert.Equal(1, fakeNavigator.OpenBackOfficeCallCount);
    }

    [Fact]
    public void PosNavigationGuardDialog_InitializesWithCorrectCaptionsAndButtons()
    {
        using var dialog = new PosNavigationGuardDialog();

        Assert.Equal("Order in Progress", dialog.Text);
        Assert.NotNull(dialog.AcceptButton);
        Assert.NotNull(dialog.CancelButton);
        Assert.Equal(DialogResult.Yes, dialog.AcceptButton.DialogResult);
        Assert.Equal(DialogResult.Cancel, dialog.CancelButton.DialogResult);
    }
}
