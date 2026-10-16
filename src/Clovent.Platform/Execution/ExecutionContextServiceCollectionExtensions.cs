using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Platform.Execution;

/// <summary>DI registration for the Execution Context feature.</summary>
public static class ExecutionContextServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default <see cref="ExecutionContextAccessor"/> as the
    /// singleton <see cref="IExecutionContextAccessor"/>, if one isn't
    /// already registered. Called by
    /// <see cref="DependencyInjection.InfrastructureServiceCollectionExtensions.AddInfrastructure"/>;
    /// most hosts won't need to call this directly.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddExecutionContextAccessor(this IServiceCollection services)
    {
        services.TryAddSingleton<IExecutionContextAccessor, ExecutionContextAccessor>();
        return services;
    }

    /// <summary>
    /// Registers trusted scope enforcement, background scope service, and the MediatR scope pipeline behavior.
    /// </summary>
    public static IServiceCollection AddScopeEnforcement(this IServiceCollection services)
    {
        services.AddExecutionContextAccessor();
        services.TryAddScoped<ITrustedScopeResolver, AuthoritativeScopeResolver>();
        services.TryAddSingleton<IBackgroundExecutionScopeService, BackgroundExecutionScopeService>();
        services.AddScoped(typeof(MediatR.IPipelineBehavior<,>), typeof(ScopeEnforcementBehavior<,>));
        return services;
    }
}
