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
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task FailedInitialization_DisposesOwnedSingletons(bool persistenceFailure, bool cancelled)
    {
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", ValidPlatformSection);
        var bootstrapper = ApplicationBootstrapper.Create(basePath: dir.Path);
        var resource = new TrackedResource();
        var failure = cancelled ? (Exception)new OperationCanceledException() : new InvalidOperationException("Synthetic startup failure");
        bootstrapper.Services.AddSingleton<TrackedResource>(_ => resource);
        if (persistenceFailure)
            bootstrapper.Services.AddScoped<IPersistenceInitializer>(sp => new FailingInitializer(sp.GetRequiredService<TrackedResource>(), failure));
        else
            bootstrapper.Services.AddScoped<IStartupTask>(sp => new FailingStartupTask(sp.GetRequiredService<TrackedResource>(), failure));

        var actual = await Record.ExceptionAsync(() => bootstrapper.BuildAndInitializeAsync());

        Assert.Same(failure, actual);
        Assert.True(resource.Disposed);
    }

    private sealed class TrackedResource : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    private sealed class FailingInitializer(TrackedResource resource, Exception failure) : IPersistenceInitializer
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            GC.KeepAlive(resource);
            return Task.FromException(failure);
        }
    }

    private sealed class FailingStartupTask(TrackedResource resource, Exception failure) : IStartupTask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            GC.KeepAlive(resource);
            return Task.FromException(failure);
        }
    }

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
