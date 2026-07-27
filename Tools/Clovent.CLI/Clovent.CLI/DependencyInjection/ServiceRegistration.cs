namespace Clovent.CLI.DependencyInjection;

using Clovent.Core.Interfaces;
using Clovent.Core.Services;
using Clovent.Documents;
using Clovent.Generator.Services.Entity;
using Clovent.Generator.Services.Module;
using Clovent.Templates;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddCloventServices(this IServiceCollection services)
    {
        // Core
        services.AddSingleton<IBootstrapService, BootstrapService>();
        services.AddSingleton<IDoctorService, DoctorService>();

        // Templates
        services.AddSingleton<ITemplateEngine, TemplateEngine>();

        // Documents
        services.AddSingleton<IObsidianVaultManager, ObsidianVaultManager>();
        services.AddSingleton<IDocumentGenerator, DocumentGenerator>();

        // Generators
        services.AddSingleton<IModuleGenerator, ModuleGenerator>();
        services.AddSingleton<IEntityGenerator, EntityGenerator>();

        return services;
    }
}
