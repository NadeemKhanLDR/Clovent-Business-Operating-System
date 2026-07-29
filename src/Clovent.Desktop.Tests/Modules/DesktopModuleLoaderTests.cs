using Clovent.Desktop.Modules;
using Clovent.Platform.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Modules;

public class DesktopModuleLoaderTests
{
    private sealed class FakeModuleMarker;

    private sealed class FakeModule : IModule
    {
        public string Name => "Fake";

        public void RegisterServices(IServiceCollection services, IConfiguration configuration) =>
            services.AddSingleton<FakeModuleMarker>();
    }

    private sealed class ModuleWithoutParameterlessConstructor(string name) : IModule
    {
        public string Name { get; } = name;

        public void RegisterServices(IServiceCollection services, IConfiguration configuration)
        {
        }
    }

    private static IConfiguration EmptyConfiguration => new ConfigurationBuilder().Build();

    [Fact]
    public void LoadModules_RegistersModuleAndRunsItsRegisterServices()
    {
        var services = new ServiceCollection();

        services.LoadModules(EmptyConfiguration, [typeof(FakeModule)]);

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ModuleRegistry>();

        Assert.True(registry.IsRegistered("Fake"));
        Assert.NotNull(provider.GetService<FakeModuleMarker>());
    }

    [Fact]
    public void LoadModules_EmptyList_RegistersNothing()
    {
        var services = new ServiceCollection();

        services.LoadModules(EmptyConfiguration, []);

        using var provider = services.BuildServiceProvider();

        Assert.Null(provider.GetService<ModuleRegistry>());
    }

    [Fact]
    public void LoadModules_TypeNotImplementingIModule_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.LoadModules(EmptyConfiguration, [typeof(string)]));
    }

    [Fact]
    public void LoadModules_TypeWithoutParameterlessConstructor_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.LoadModules(EmptyConfiguration, [typeof(ModuleWithoutParameterlessConstructor)]));
    }
}
