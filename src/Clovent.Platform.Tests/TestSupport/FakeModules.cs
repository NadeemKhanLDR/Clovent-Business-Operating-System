using Clovent.Platform.Bootstrap;
using Clovent.Platform.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Platform.Tests.TestSupport;

public interface ITestModuleMarker
{
    string ModuleName { get; }
}

public sealed class FakeModuleA : IModule
{
    public string Name => "TestModuleA";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITestModuleMarker>(new FakeModuleMarker(Name));
    }
}

public sealed class FakeModuleB : IModule
{
    public string Name => "TestModuleB";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ITestModuleMarker>(new FakeModuleMarker(Name));
    }
}

/// <summary>Registers under the same Name as <see cref="FakeModuleA"/> to exercise duplicate detection.</summary>
public sealed class FakeDuplicateOfModuleA : IModule
{
    public string Name => "TestModuleA";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
    }
}

public sealed record FakeModuleMarker(string ModuleName) : ITestModuleMarker;

public sealed class FakePersistenceInitializer : IPersistenceInitializer
{
    public static bool WasInitialized { get; set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        WasInitialized = true;
        return Task.CompletedTask;
    }
}

public sealed class FakeStartupTask : IStartupTask
{
    public static bool WasExecuted { get; set; }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        WasExecuted = true;
        return Task.CompletedTask;
    }
}

public sealed class FakeModuleWithStartupWork : IModule
{
    public string Name => "TestModuleWithStartupWork";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPersistenceInitializer, FakePersistenceInitializer>();
        services.AddSingleton<IStartupTask, FakeStartupTask>();
    }
}
