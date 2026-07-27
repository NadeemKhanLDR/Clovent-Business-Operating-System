using Clovent.Platform.Configuration;
using Clovent.Platform.Tests.TestSupport;
using Xunit;

namespace Clovent.Platform.Tests.Configuration;

public sealed class PlatformConfigurationTests
{
    [Fact]
    public void Build_ReadsBaseAppsettings_WhenNoOtherSourceOverrides()
    {
        var key = "Setting_" + Guid.NewGuid().ToString("N");
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", $$"""{ "{{key}}": "base" }""");

        var configuration = PlatformConfiguration.Build(dir.Path);

        Assert.Equal("base", configuration[key]);
    }

    [Fact]
    public void Build_EnvironmentSpecificFile_OverridesBaseFile()
    {
        var key = "Setting_" + Guid.NewGuid().ToString("N");
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", $$"""{ "{{key}}": "base" }""");
        dir.WriteFile("appsettings.Development.json", $$"""{ "{{key}}": "development" }""");

        var configuration = PlatformConfiguration.Build(dir.Path, environmentName: "Development");

        Assert.Equal("development", configuration[key]);
    }

    [Fact]
    public void Build_EnvironmentVariable_OverridesJsonFiles()
    {
        var key = "Setting_" + Guid.NewGuid().ToString("N");
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", $$"""{ "{{key}}": "base" }""");
        dir.WriteFile("appsettings.Development.json", $$"""{ "{{key}}": "development" }""");

        System.Environment.SetEnvironmentVariable(key, "from-env");
        try
        {
            var configuration = PlatformConfiguration.Build(dir.Path, environmentName: "Development");

            Assert.Equal("from-env", configuration[key]);
        }
        finally
        {
            System.Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Fact]
    public void Build_CommandLine_OverridesEverythingElse()
    {
        var key = "Setting_" + Guid.NewGuid().ToString("N");
        using var dir = new TempDirectory();
        dir.WriteFile("appsettings.json", $$"""{ "{{key}}": "base" }""");
        dir.WriteFile("appsettings.Development.json", $$"""{ "{{key}}": "development" }""");

        System.Environment.SetEnvironmentVariable(key, "from-env");
        try
        {
            var configuration = PlatformConfiguration.Build(
                dir.Path,
                environmentName: "Development",
                commandLineArgs: [$"--{key}=from-cli"]);

            Assert.Equal("from-cli", configuration[key]);
        }
        finally
        {
            System.Environment.SetEnvironmentVariable(key, null);
        }
    }
}
