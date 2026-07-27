using Clovent.CLI.Commands.Bootstrap;
using Clovent.CLI.Commands.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.CLI.Extensions;

public static class CommandRegistration
{
    public static IServiceCollection AddCliCommands(
        this IServiceCollection services)
    {
        services.AddSingleton<BootstrapCommand>();

        services.AddSingleton<DoctorCommand>();

        return services;
    }
}
