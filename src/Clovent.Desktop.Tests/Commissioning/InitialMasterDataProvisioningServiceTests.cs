using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Forms.Base;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.MasterData.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class InitialMasterDataProvisioningServiceTests : IDisposable
{
    private readonly SqliteConnection _identityConn;
    private readonly SqliteConnection _masterDataConn;
    private readonly ServiceProvider _serviceProvider;
    private readonly IdentityDbContext _identityDbContext;
    private readonly MasterDataDbContext _masterDataDbContext;
    private readonly InitialMasterDataProvisioningService _sut;

    public InitialMasterDataProvisioningServiceTests()
    {
        _identityConn = new SqliteConnection("DataSource=:memory:");
        _identityConn.Open();
        _masterDataConn = new SqliteConnection("DataSource=:memory:");
        _masterDataConn.Open();

        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_identityConn)
            .Options;

        var masterDataOptions = new DbContextOptionsBuilder<MasterDataDbContext>()
            .UseSqlite(_masterDataConn)
            .Options;

        _identityDbContext = new IdentityDbContext(identityOptions);
        _identityDbContext.Database.EnsureCreated();

        _masterDataDbContext = new MasterDataDbContext(masterDataOptions);
        _masterDataDbContext.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddScoped(_ => new IdentityDbContext(identityOptions));
        services.AddScoped(_ => new MasterDataDbContext(masterDataOptions));

        _serviceProvider = services.BuildServiceProvider();
        _sut = new InitialMasterDataProvisioningService();

        CompanyDisplaySettingsStore.ResetCacheForTesting();
        PosSettingsStore.ResetCacheForTesting();
    }

    public void Dispose()
    {
        _identityDbContext.Dispose();
        _masterDataDbContext.Dispose();
        _identityConn.Dispose();
        _masterDataConn.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task ProvisionInitialMasterDataAsync_CreatesCompleteHierarchy_OnFreshDatabase()
    {
        // Arrange
        var request = new InitialMasterDataProvisioningRequest
        {
            OrganizationName = "Apex Retail Group",
            OrganizationCode = "ORG-APEX",
            CompanyName = "Apex Operating LLC",
            CompanyCode = "COMP-APEX",
            BranchName = "Flagship Store",
            BranchCode = "BR-FLAG",
            WarehouseName = "Central Depot",
            WarehouseCode = "WH-CD",
            TerminalName = "Register 101",
            TerminalCode = "T-101",
            HardwareId = "TEST-HWID-1234-5678",
            CurrencyCode = "EUR",
            CurrencyName = "Euro",
            CurrencySymbol = "€",
            CurrencyPrecision = 2,
            LanguageCode = "es",
            LanguageName = "Spanish",
            TimeZoneIanaId = "UTC",
            TimeZoneDisplayName = "UTC",
            DateFormat = "dd/MM/yyyy",
            TimeFormat = "24 Hour",
            QuantityPrecision = 3
        };

        // Act
        var result = await _sut.ProvisionInitialMasterDataAsync(_serviceProvider, request);

        // Assert
        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.OrganizationId);
        Assert.NotEqual(Guid.Empty, result.CompanyId);
        Assert.NotEqual(Guid.Empty, result.BranchId);
        Assert.NotEqual(Guid.Empty, result.WarehouseId);
        Assert.NotEqual(Guid.Empty, result.TerminalId);
        Assert.NotEqual(Guid.Empty, result.BusinessSettingsId);
        Assert.Equal("TEST-HWID-1234-5678", result.HardwareId);

        // Verify entities in database
        Assert.Equal(1, await _identityDbContext.Organizations.CountAsync());
        Assert.Equal(1, await _identityDbContext.Companies.CountAsync());
        Assert.Equal(1, await _identityDbContext.Branches.CountAsync());
        Assert.Equal(1, await _masterDataDbContext.Warehouses.CountAsync());
        Assert.Equal(1, await _masterDataDbContext.Terminals.CountAsync());
        Assert.Equal(1, await _masterDataDbContext.BusinessSettings.CountAsync());

        // Verify local workstation settings stores
        var displaySettings = CompanyDisplaySettingsStore.Load();
        Assert.Equal("dd/MM/yyyy", displaySettings.DateFormat);
        Assert.Equal("24 Hour", displaySettings.TimeFormat);
        Assert.Equal(3, displaySettings.QuantityPrecision);

        Assert.Equal(result.TerminalId, PosSettingsStore.LoadTerminalId());
        Assert.Equal(result.BranchId, PosSettingsStore.LoadBranchId());
    }

    [Fact]
    public async Task ProvisionInitialMasterDataAsync_IsIdempotent_WhenExecutedRepeatedly()
    {
        // Arrange
        var request = new InitialMasterDataProvisioningRequest
        {
            OrganizationName = "Global Food Corp",
            CompanyName = "Global Food Services",
            BranchName = "City Center",
            WarehouseName = "Main Store",
            WarehouseCode = "WH-01",
            TerminalName = "POS Station 1",
            TerminalCode = "T-001"
        };

        // Act: Execute twice
        var firstRun = await _sut.ProvisionInitialMasterDataAsync(_serviceProvider, request);
        var secondRun = await _sut.ProvisionInitialMasterDataAsync(_serviceProvider, request);

        // Assert: Both succeed, exact same IDs reused, no duplicate entities
        Assert.True(firstRun.Success);
        Assert.True(secondRun.Success);

        Assert.Equal(firstRun.OrganizationId, secondRun.OrganizationId);
        Assert.Equal(firstRun.CompanyId, secondRun.CompanyId);
        Assert.Equal(firstRun.BranchId, secondRun.BranchId);
        Assert.Equal(firstRun.WarehouseId, secondRun.WarehouseId);
        Assert.Equal(firstRun.TerminalId, secondRun.TerminalId);

        Assert.Equal(1, await _identityDbContext.Organizations.CountAsync());
        Assert.Equal(1, await _identityDbContext.Companies.CountAsync());
        Assert.Equal(1, await _identityDbContext.Branches.CountAsync());
        Assert.Equal(1, await _masterDataDbContext.Warehouses.CountAsync());
        Assert.Equal(1, await _masterDataDbContext.Terminals.CountAsync());
    }
}
