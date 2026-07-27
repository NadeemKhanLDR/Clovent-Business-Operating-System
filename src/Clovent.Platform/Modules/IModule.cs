using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Platform.Modules;

/// <summary>
/// Contract every future CBOS module implements to self-register with the
/// host. No central switch statement or manual registration list is ever
/// needed - see <see cref="ModuleServiceCollectionExtensions.AddModule{TModule}"/>.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Unique, stable name for this module (e.g. "Identity", "RestaurantPOS").
    /// Used by <see cref="ModuleRegistry"/> to detect duplicate registrations
    /// and by anything that needs to identify a module at runtime; not
    /// necessarily the same as the module's assembly or namespace name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers everything this module needs in the DI container -
    /// typically by calling the module's own AddApplication()/
    /// AddInfrastructure()/AddPersistence() in turn. Called once, at
    /// registration time (<see cref="Modules.ModuleServiceCollectionExtensions.AddModule{TModule}"/>),
    /// before the container is built.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configuration">The host's configuration, for any settings this module needs at registration time (e.g. a connection string).</param>
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
