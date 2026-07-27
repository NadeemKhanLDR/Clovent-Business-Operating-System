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
    string Name { get; }

    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
