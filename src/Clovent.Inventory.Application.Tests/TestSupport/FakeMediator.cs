using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Clovent.Inventory.Application.Tests.TestSupport;

/// <summary>
/// Minimal IMediator stand-in for testing query/command handlers that invoke cross-context queries.
/// </summary>
internal sealed class FakeMediator : IMediator
{
    private readonly Func<object, Task<object?>> _handler;

    public FakeMediator(Func<object, Task<object?>>? handler = null) =>
        _handler = handler ?? (_ => Task.FromResult<object?>(null));

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
