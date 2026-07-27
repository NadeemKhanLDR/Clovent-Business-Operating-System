using Clovent.Platform.Bootstrap;
using Clovent.Platform.Configuration;
using Clovent.Platform.Execution;
using Clovent.Platform.Modules;
using Clovent.Platform.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Clovent.Platform.Tests.Bootstrap;

public sealed class ApplicationBootstrapperTests
{
    private const string ValidPlatformSection = """
        {
          "Platform": {
            "EnvironmentName": "Test",
            "DefaultCulture": "en-US",
            "DefaultTimeZone": "UTC",
            "DefaultCurrency": "USD"
          }
        }
        """;

    [Fact]
    public void Build_WithCompleteConfiguration_ResolvesPlatformServices()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", ValidPlatformSection);

        using var host = ApplicationBootstrapper
            .Create(basePath: dir.Path)
            .WithLogging()
            .WithPlatform()
            .Build();

        Assert.NotNull(host.Services.GetRequiredService<ModuleRegistry>());
        Assert.NotNull(host.Services.GetRequiredService<IExecutionContextAccessor>());
        Assert.Equal("en-US", host.Services.GetRequiredService<IOptions<PlatformOptions>>().Value.DefaultCulture);
    }

    [Fact]
    public async Task StartAsync_WithIncompletePlatformConfiguration_FailsValidationOnStart()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", """{ "Platform": { "EnvironmentName": "Test" } }""");

        using var host = ApplicationBootstrapper
            .Create(basePath: dir.Path)
            .WithPlatform()
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    [Fact]
    public async Task StartAsync_WithCompletePlatformConfiguration_Succeeds()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", ValidPlatformSection);

        using var host = ApplicationBootstrapper
            .Create(basePath: dir.Path)
            .WithPlatform()
            .Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    [Fact]
    public void WithModule_RegistersModule_ViaAddModuleTModuleOnly()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", ValidPlatformSection);

        using var host = ApplicationBootstrapper
            .Create(basePath: dir.Path)
            .WithPlatform()
            .WithModule<FakeModuleA>()
            .Build();

        var registry = host.Services.GetRequiredService<ModuleRegistry>();
        Assert.True(registry.IsRegistered("TestModuleA"));
    }

    [Fact]
    public async Task BuildAndInitializeAsync_RunsPersistenceInitializersAndStartupTasks()
    {
        FakePersistenceInitializer.WasInitialized = false;
        FakeStartupTask.WasExecuted = false;

        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", ValidPlatformSection);

        using var host = await ApplicationBootstrapper
            .Create(basePath: dir.Path)
            .WithPlatform()
            .WithModule<FakeModuleWithStartupWork>()
            .BuildAndInitializeAsync();

        Assert.True(FakePersistenceInitializer.WasInitialized);
        Assert.True(FakeStartupTask.WasExecuted);
    }
}
