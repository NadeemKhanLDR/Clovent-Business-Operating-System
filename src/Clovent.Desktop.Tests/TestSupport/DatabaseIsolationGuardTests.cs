using System;
using System.Collections.Generic;
using Clovent.Desktop.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Clovent.Desktop.Tests.TestSupport;

public sealed class DatabaseIsolationGuardTests
{
    [Fact]
    public void IsOperationalDatabase_IdentifiesOperationalDatabaseName()
    {
        var opConnStr = "Server=localhost;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;";
        Assert.True(DatabaseIsolationGuard.IsOperationalDatabase(opConnStr));

        var opConnStrCaseInsensitive = "Server=localhost;Database=clovent_businessoperatingsystem;Trusted_Connection=True;";
        Assert.True(DatabaseIsolationGuard.IsOperationalDatabase(opConnStrCaseInsensitive));
    }

    [Fact]
    public void IsOperationalDatabase_RejectsMissingCatalog()
    {
        var missingCatalog = "Server=localhost;Trusted_Connection=True;";
        Assert.True(DatabaseIsolationGuard.IsOperationalDatabase(missingCatalog));
    }

    [Fact]
    public void IsOperationalDatabase_AcceptsExplicitDisposableDatabase()
    {
        var disposableConnStr = "Server=localhost;Database=CBOS_DisposableTestDb_1234;Trusted_Connection=True;";
        Assert.False(DatabaseIsolationGuard.IsOperationalDatabase(disposableConnStr));
    }

    [Fact]
    public void EnsureIsolated_ThrowsOnOperationalDatabase()
    {
        var opConnStr = "Server=localhost;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;";
        var ex = Assert.Throws<InvalidOperationException>(() => DatabaseIsolationGuard.EnsureIsolated(opConnStr));
        Assert.Contains("CRITICAL ISOLATION BREACH", ex.Message);
    }

    [Fact]
    public void ApplyToConfiguration_PopulatesAllModuleConnectionStrings()
    {
        var disposableConnStr = "Server=localhost;Database=CBOS_Disposable_QA;Trusted_Connection=True;";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        DatabaseIsolationGuard.ApplyToConfiguration(config, disposableConnStr);

        foreach (var key in DatabaseIsolationGuard.KnownConnectionStringKeys)
        {
            Assert.Equal(disposableConnStr, config[$"ConnectionStrings:{key}"]);
        }
    }
}
