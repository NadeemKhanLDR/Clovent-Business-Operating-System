using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Restaurant.Application;
using Clovent.Restaurant.Application.KitchenTickets.Dtos;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using MediatR;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Regression coverage for the Kitchen Tickets crash: one stale
/// KitchenTicket whose Order row no longer exists (orphaned by an earlier
/// database reset) used to make <see cref="KitchenTicketViewerView"/>'s
/// load throw "Order '...' was not found" and take the whole screen down.
/// </summary>
public class KitchenTicketViewerViewTests
{
    private static KitchenTicketDto Ticket(Guid orderId, string status = "New") =>
        new(Guid.NewGuid(), orderId, [Guid.NewGuid()], status, DateTimeOffset.UtcNow, null, null, null);

    private sealed class ThrowingForMissingOrdersMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            request is GetOrderByIdQuery
                ? throw new NotFoundException("Order", ((GetOrderByIdQuery)(object)request).OrderId)
                : throw new NotSupportedException("Unexpected request in test.");

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            throw new NotSupportedException("Unexpected request in test.");

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Unexpected request in test.");

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Unexpected notification in test.");

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
            throw new NotSupportedException("Unexpected notification in test.");

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Unexpected stream in test.");

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Unexpected stream in test.");
    }

    [Fact]
    public async Task BuildRows_OrderMissing_ShowsUnavailableInsteadOfThrowing()
    {
        var tickets = new[] { Ticket(Guid.NewGuid()) };

        var rows = await KitchenTicketViewerView.BuildRowsAsync(new ThrowingForMissingOrdersMediator(), tickets, CancellationToken.None);

        var row = Assert.Single(rows);
        Assert.Equal("(order unavailable)", row.OrderNumber);
    }
}
