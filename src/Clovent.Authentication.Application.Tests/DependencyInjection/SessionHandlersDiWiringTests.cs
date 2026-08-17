using Clovent.Authentication.Application.DependencyInjection;
using Clovent.Authentication.Application.Sessions.Commands;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Authentication.Application.Tests.DependencyInjection;

/// <summary>
/// Verifies that <see cref="ApplicationServiceCollectionExtensions.AddApplication"/> registers
/// every type the session-termination handlers depend on, including
/// <see cref="Sessions.SessionTerminationCascade"/> which is not a MediatR interface
/// implementor and therefore not picked up automatically by
/// <c>AddMediatR.RegisterServicesFromAssembly</c>.
/// Added as a regression guard after the runtime DI startup failure caused by
/// the missing SessionTerminationCascade registration.
/// </summary>
public class SessionHandlersDiWiringTests
{
    private static IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        // Stub configuration - AddApplication does not read any keys today.
        var config = new ConfigurationBuilder().Build();

        // Register the Authentication Application layer (includes
        // SessionTerminationCascade and all MediatR handlers).
        services.AddApplication(config);

        // Register fake implementations of the Scoped repository interfaces
        // that SessionTerminationCascade and the handlers depend on so the
        // container can be built and resolved without a real database.
        services.AddScoped<ISessionRepository, FakeSessionRepository>();
        services.AddScoped<IRefreshSessionRepository, FakeRefreshSessionRepository>();

        // TimeProvider is normally supplied by the host; supply a concrete
        // stand-in for this isolated test container.
        services.AddSingleton<TimeProvider>(TimeProvider.System);

        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public void AddApplication_ExpireSessionCommandHandler_Resolves()
    {
        using var scope = BuildProvider().CreateScope();
        // Must not throw - previously failed with
        // "Unable to resolve service for type SessionTerminationCascade"
        var handler = scope.ServiceProvider
            .GetRequiredService<MediatR.IRequestHandler<ExpireSessionCommand>>();
        Assert.IsType<ExpireSessionCommandHandler>(handler);
    }

    [Fact]
    public void AddApplication_LogOutSessionCommandHandler_Resolves()
    {
        using var scope = BuildProvider().CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<MediatR.IRequestHandler<LogOutSessionCommand>>();
        Assert.IsType<LogOutSessionCommandHandler>(handler);
    }

    [Fact]
    public void AddApplication_RevokeSessionCommandHandler_Resolves()
    {
        using var scope = BuildProvider().CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<MediatR.IRequestHandler<RevokeSessionCommand>>();
        Assert.IsType<RevokeSessionCommandHandler>(handler);
    }
}
