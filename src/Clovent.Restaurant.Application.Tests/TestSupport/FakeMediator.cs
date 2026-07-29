using MediatR;

namespace Clovent.Restaurant.Application.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IMediator"/> stand-in for testing handlers that call
/// into another bounded context's Application layer via <c>IMediator.Send</c>
/// (<c>AddOrderLineCommandHandler</c> into Catalog, <c>CompleteOrderCommandHandler</c>
/// into Inventory) - only <see cref="Send{TResponse}"/> is exercised by this
/// project's handlers, so every other <see cref="IMediator"/> member throws.
/// </summary>
internal sealed class FakeMediator : IMediator
{
    private readonly Func<object, Task<object?>> _handler;

    public FakeMediator(Func<object, Task<object?>> handler) => _handler = handler;

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        (TResponse)(await _handler(request))!;

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) => _handler(request);

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
        throw new NotSupportedException();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task Publish(object notification, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
        throw new NotSupportedException();
}
