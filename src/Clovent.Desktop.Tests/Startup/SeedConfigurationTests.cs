using Clovent.Desktop.Theming;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Clovent.Desktop.Tests.Startup;

/// <summary>
/// Guards the production-configuration security invariant: the base
/// appsettings.json (the only configuration a default/Production
/// installation resolves - the generic host defaults the environment to
/// "Production" when DOTNET_ENVIRONMENT is unset) must leave every
/// SeedDevelopment* flag off, so no client install ever auto-seeds the
/// demo admin account or demo data. Development seeding lives exclusively
/// in the appsettings.Development.json overlay (applied only when the
/// environment is explicitly "Development", e.g. via launchSettings.json).
/// </summary>
public class SeedConfigurationTests
{
    private static readonly string ConfigDirectory = Path.Combine(
        AppContext.BaseDirectory, "Config");

    private static IConfiguration BuildConfiguration(string environmentName)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(ConfigDirectory)
            .AddJsonFile("appsettings.json", optional: false);

        var overlay = Path.Combine(ConfigDirectory, $"appsettings.{environmentName}.json");
        if (File.Exists(overlay))
        {
            builder.AddJsonFile($"appsettings.{environmentName}.json", optional: false);
        }

        return builder.Build();
    }

    private static DesktopOptions BindDesktopOptions(IConfiguration configuration)
    {
        var options = configuration.GetSection(DesktopOptions.SectionName).Get<DesktopOptions>();
        Assert.NotNull(options);
        return options!;
    }

    [Fact]
    public void ProductionConfiguration_DisablesAllDevelopmentSeeds()
    {
        // No DOTNET_ENVIRONMENT / default environment: only the base
        // appsettings.json applies (no appsettings.Production.json exists).
        var options = BindDesktopOptions(BuildConfiguration("Production"));

        Assert.False(options.SeedDevelopmentUser);
        Assert.False(options.SeedDevelopmentMasterData);
        Assert.False(options.SeedDevelopmentCatalogData);
        Assert.False(options.SeedDevelopmentRestaurantData);
    }

    [Fact]
    public void ProductionConfiguration_DoesNotDeclareDevelopmentEnvironment()
    {
        var configuration = BuildConfiguration("Production");

        Assert.NotEqual("Development", configuration["Platform:EnvironmentName"]);
    }

    [Fact]
    public void DevelopmentConfiguration_EnablesAllDevelopmentSeeds()
    {
        var options = BindDesktopOptions(BuildConfiguration("Development"));

        Assert.True(options.SeedDevelopmentUser);
        Assert.True(options.SeedDevelopmentMasterData);
        Assert.True(options.SeedDevelopmentCatalogData);
        Assert.True(options.SeedDevelopmentRestaurantData);
        Assert.Equal("Development", BuildConfiguration("Development")["Platform:EnvironmentName"]);
    }
}
