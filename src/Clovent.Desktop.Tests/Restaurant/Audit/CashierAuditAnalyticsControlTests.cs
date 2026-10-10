using System;
using Clovent.Desktop.Restaurant.Audit;
using Clovent.Desktop.Sessions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Audit;

public sealed class CashierAuditAnalyticsControlTests
{
    [Fact]
    public void DesignerConstructor_InitializesWithoutThrowing()
    {
        // Act & Assert
#pragma warning disable CS0618, CS0619 // Type or member is obsolete
        using var control = new CashierAuditAnalyticsControl();
#pragma warning restore CS0618, CS0619
        Assert.NotNull(control);
        Assert.Equal(System.Windows.Forms.DockStyle.Fill, control.Dock);
    }

    [Fact]
    public void RuntimeConstructor_InitializesControlsAndTabs()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IMediator>(new FakeMediator());

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var session = new FakeCurrentSession();

        // Act
        using var control = new CashierAuditAnalyticsControl(scopeFactory, session);

        // Assert
        Assert.NotNull(control);
        Assert.True(control.Controls.Count > 0);
    }

    private sealed class FakeMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) =>
            Task.FromResult(default(TResponse)!);

        public Task Send<TRequest>(TRequest request, System.Threading.CancellationToken cancellationToken = default) where TRequest : IRequest =>
            Task.CompletedTask;

        public Task<object?> Send(object request, System.Threading.CancellationToken cancellationToken = default) =>
            Task.FromResult<object?>(null);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, System.Threading.CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task Publish(object notification, System.Threading.CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, System.Threading.CancellationToken cancellationToken = default) where TNotification : INotification =>
            Task.CompletedTask;
    }

    private sealed class FakeCurrentSession : ICurrentSession
    {
        public Guid? UserId => Guid.NewGuid();
        public Guid? SessionId => Guid.NewGuid();
        public string? UserName => "admin";
        public string? DisplayName => "Administrator";
        public string? UserDisplayName => "Administrator";
        public Guid? CompanyId => Guid.NewGuid();
        public Guid? BranchId => Guid.NewGuid();
        public Guid? TerminalId => Guid.NewGuid();
        public Guid? WarehouseId => Guid.NewGuid();
        public string? TerminalCode => "POS01";
        public bool IsAuthenticated => true;
        public IReadOnlySet<string> Permissions => new HashSet<string> { "menu.audit.analytics" };
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
        public event EventHandler? Changed { add { } remove { } }
    }
}
