using Clovent.Platform.Modules;
using Clovent.Platform.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Platform.Tests.Modules;

public sealed class ModuleRegistryTests
{
    private static IConfiguration EmptyConfiguration()
        => new ConfigurationBuilder().Build();

    [Fact]
    public void AddModule_RegistersModule_DiscoverableViaModuleRegistry()
    {
        var services = new ServiceCollection();
        services.AddModule<FakeModuleA>(EmptyConfiguration());

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ModuleRegistry>();

        Assert.True(registry.IsRegistered("TestModuleA"));
        Assert.Single(registry.RegisteredModules);
    }

    [Fact]
    public void AddModule_AlsoRunsRegisterServices_SoModuleServicesAreAvailable()
    {
        var services = new ServiceCollection();
        services.AddModule<FakeModuleA>(EmptyConfiguration());

        var provider = services.BuildServiceProvider();
        var marker = provider.GetRequiredService<ITestModuleMarker>();

        Assert.Equal("TestModuleA", marker.ModuleName);
    }

    [Fact]
    public void MultipleModules_AllAppearInRegistry_WithNoManualList()
    {
        var services = new ServiceCollection();
        services.AddModule<FakeModuleA>(EmptyConfiguration());
        services.AddModule<FakeModuleB>(EmptyConfiguration());

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<ModuleRegistry>();

        Assert.Equal(2, registry.RegisteredModules.Count);
        Assert.True(registry.IsRegistered("TestModuleA"));
        Assert.True(registry.IsRegistered("TestModuleB"));
    }

    [Fact]
    public void DuplicateModuleName_ThrowsOnRegistryResolution()
    {
        var services = new ServiceCollection();
        services.AddModule<FakeModuleA>(EmptyConfiguration());
        services.AddModule<FakeDuplicateOfModuleA>(EmptyConfiguration());

        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ModuleRegistry>());
    }
}
