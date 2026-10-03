using System.Text.Json;
using Clovent.Desktop.Commissioning.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class CommissioningStateServiceTests
{
    private readonly CommissioningStateService _sut = new();

    [Fact]
    public void CommissioningStatusResult_IsCommissioned_RequiresAllFiveSignals()
    {
        var result = new CommissioningStatusResult
        {
            Signal1DatabaseConnected = true,
            Signal2SchemaMigrated = true,
            Signal3ActiveAdminExists = true,
            Signal4MasterDataExists = true,
            Signal5MarkerExists = false
        };

        // Any missing signal -> false
        Assert.False(result.IsCommissioned);

        result.Signal5MarkerExists = true;
        // All 5 signals -> true
        Assert.True(result.IsCommissioned);
    }

    [Fact]
    public async Task CheckCommissioningStateAsync_ReturnsNotCommissioned_WhenNoDbConfigured()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();

        // Act
        var result = await _sut.CheckCommissioningStateAsync(services);

        // Assert
        Assert.False(result.IsCommissioned);
        Assert.NotNull(result.MissingDetailsMessage);
    }

    [Fact]
    public async Task RecordCommissioningCompleteAsync_WritesValidMarker_ThatPassesVerification()
    {
        // Arrange
        var summary = new CommissioningSummary
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            MachineId = "TEST-MACH-1111-2222",
            AppVersion = "2.0.0.0",
            DatabaseName = "Clovent_UnitTestingDb",
            ServerName = "localhost",
            OrganizationName = "Test Corp",
            CompanyName = "Test Operations LLC",
            BranchName = "Test Branch",
            AdminUserName = "testadmin",
            TerminalName = "Register 1"
        };

        // Act
        await _sut.RecordCommissioningCompleteAsync(summary);

        // Assert
        var record = CommissioningStateService.LoadMarker();
        Assert.NotNull(record);
        Assert.Equal("Test Corp", record.OrganizationName);
        Assert.Equal("Clovent_UnitTestingDb", record.DatabaseName);
        Assert.NotEmpty(record.IntegritySignature);
    }

    [Fact]
    public async Task VerifyCommissioningMarker_DetectsTampering_WhenFileIsModified()
    {
        // Arrange: Write a valid marker
        var summary = new CommissioningSummary
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            MachineId = "TEST-MACH-3333-4444",
            AppVersion = "2.0.0.0",
            DatabaseName = "Clovent_TamperTestDb",
            OrganizationName = "Original Org"
        };
        await _sut.RecordCommissioningCompleteAsync(summary);

        // Tamper with the written file by modifying OrganizationName without updating the HMAC
        string userPath = CommissioningStateService.FallbackMarkerFilePath;
        string machinePath = CommissioningStateService.MarkerFilePath;
        string targetPath = File.Exists(machinePath) ? machinePath : userPath;

        var json = await File.ReadAllTextAsync(targetPath);
        var marker = JsonSerializer.Deserialize<CommissioningMarker>(json)!;
        marker.OrganizationName = "Tampered Hacker Org";

        var tamperedJson = JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(targetPath, tamperedJson);

        // Act: Verify tampered marker
        var loaded = CommissioningStateService.LoadMarker();

        // Assert: Cryptographic verification must reject tampered content
        Assert.Null(loaded);
    }
}
