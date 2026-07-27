using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clovent.Platform.Execution;

public static class ExecutionContextServiceCollectionExtensions
{
    public static IServiceCollection AddExecutionContextAccessor(this IServiceCollection services)
    {
        services.TryAddSingleton<IExecutionContextAccessor, ExecutionContextAccessor>();
        return services;
    }
}
