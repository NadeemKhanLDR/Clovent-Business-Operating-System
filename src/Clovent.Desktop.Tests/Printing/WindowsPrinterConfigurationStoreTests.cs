using Clovent.Desktop.Printing;
using Clovent.Platform.Printing;
using Xunit;

namespace Clovent.Desktop.Tests.Printing;

public class WindowsPrinterConfigurationStoreTests : IDisposable
{
    private readonly string _tempFile;

    public WindowsPrinterConfigurationStoreTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"printer-store-test-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    [Fact]
    public async Task SaveAndLoad_PreservesProfilesAndAssignments()
    {
        // Arrange
        var store = new WindowsPrinterConfigurationStore(_tempFile);
        var profileId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var config = new PrinterConfiguration
        {
            DefaultReceiptProfileId = profileId,
            Profiles =
            [
                new PrinterProfile
                {
                    Id = profileId,
                    ProfileName = "Front Thermal 80mm",
                    SystemPrinterName = "POS-80",
                    Role = PrinterRole.Receipt,
                    ConnectionType = PrinterConnectionType.WindowsDriver,
                    PaperWidth = PaperWidth.Width80mm,
                    CharactersPerLine = 42,
                    PrintCopies = 2,
                    SupportsCutter = true,
                    SupportsCashDrawer = true
                }
            ],
            Assignments =
            [
                new PrinterAssignment
                {
                    Id = assignmentId,
                    OrganizationId = orgId,
                    Role = PrinterRole.Receipt,
                    PrinterProfileId = profileId,
                    IsDefaultReceiptPrinter = true
                }
            ]
        };

        // Act
        await store.SaveAsync(config);
        var reloadedStore = new WindowsPrinterConfigurationStore(_tempFile);
        var loaded = await reloadedStore.LoadAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(profileId, loaded.DefaultReceiptProfileId);
        Assert.Single(loaded.Profiles);
        Assert.Equal("Front Thermal 80mm", loaded.Profiles[0].ProfileName);
        Assert.Equal(42, loaded.Profiles[0].CharactersPerLine);
        Assert.Equal(2, loaded.Profiles[0].PrintCopies);
        Assert.Single(loaded.Assignments);
        Assert.Equal(orgId, loaded.Assignments[0].OrganizationId);
    }

    [Fact]
    public void NonExistentFile_ReturnsDefaultEmptyConfigurationWithoutThrowing()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid():N}.json");
        var store = new WindowsPrinterConfigurationStore(nonExistentPath);

        // Act
        var config = store.GetConfiguration();

        // Assert
        Assert.NotNull(config);
        Assert.Empty(config.Profiles);
        Assert.Empty(config.Assignments);
    }
}
