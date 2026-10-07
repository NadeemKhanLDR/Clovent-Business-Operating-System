using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class CommissioningProvisioningCoordinatorTests : IDisposable
{
    private readonly string _testDir;

    public CommissioningProvisioningCoordinatorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"cbos_comm_coord_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);

        var userDb = Path.Combine(_testDir, "user_database.config.json");
        var machineDb = Path.Combine(_testDir, "machine_database.config.json");
        var machineMarker = Path.Combine(_testDir, "machine_commissioning.json");
        var userMarker = Path.Combine(_testDir, "user_commissioning.json");

        DatabaseSecretStore.SetTestingOverrides(userDb, machineDb);
        CommissioningStateService.SetTestingOverrides(machineMarker, userMarker);
        Clovent.Desktop.Licensing.TrialStateManager.SetTestingOverrides(_testDir);
    }

    public void Dispose()
    {
        DatabaseSecretStore.ResetTestingOverrides();
        CommissioningStateService.ResetTestingOverrides();
        Clovent.Desktop.Licensing.TrialStateManager.ResetTestingOverrides();

        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
        }
    }
    private sealed class FakeBootstrapFactory : ICommissioningBootstrapFactory
    {
        public bool CreatedCalled { get; private set; }
        public string? PassedConnectionString { get; private set; }

        public ServiceProvider CreateBootstrapServiceProvider(string connectionString)
        {
            CreatedCalled = true;
            PassedConnectionString = connectionString;
            var services = new ServiceCollection();
            return services.BuildServiceProvider();
        }
    }

    private sealed class FakeMasterDataProvisioning : IInitialMasterDataProvisioningService
    {
        public bool Succeeded { get; set; } = true;
        public string Message { get; set; } = "OK";
        public Guid CompanyId { get; set; } = Guid.NewGuid();
        public Guid BranchId { get; set; } = Guid.NewGuid();

        public Task<InitialMasterDataProvisioningResult> ProvisionInitialMasterDataAsync(
            IServiceProvider services,
            InitialMasterDataProvisioningRequest request,
            CancellationToken ct = default)
        {
            return Task.FromResult(new InitialMasterDataProvisioningResult
            {
                Success = Succeeded,
                Message = Message,
                CompanyId = CompanyId,
                BranchId = BranchId
            });
        }
    }

    private sealed class FakeAdminProvisioning : IFirstAdminProvisioningService
    {
        public bool Succeeded { get; set; } = true;
        public string Message { get; set; } = "OK";
        public Guid AdminId { get; set; } = Guid.NewGuid();

        public Task<bool> CanProvisionFirstAdminAsync(
            Clovent.Identity.Infrastructure.Persistence.IdentityDbContext identityDbContext,
            CancellationToken ct = default) => Task.FromResult(true);

        public Task<(bool Success, string Message, Guid? UserId)> ProvisionFirstAdminAsync(
            IServiceProvider services,
            FirstAdminProvisioningRequest request,
            CancellationToken ct = default)
        {
            return Task.FromResult((Succeeded, Message, (Guid?)AdminId));
        }
    }

    [Fact]
    public async Task ExecuteCommissioningAsync_UsesBootstrapFactory_WhenFallbackServicesNotProvided()
    {
        // Arrange
        var fakeBootstrap = new FakeBootstrapFactory();
        var fakeMasterData = new FakeMasterDataProvisioning();
        var fakeAdmin = new FakeAdminProvisioning();

        var coordinator = new CommissioningProvisioningCoordinator(
            fakeBootstrap,
            fakeMasterData,
            fakeAdmin);

        var request = new CommissioningExecutionRequest
        {
            ConnectionSettings = new DatabaseConnectionSettings
            {
                Server = "localhost",
                Database = "TestDb",
                UseWindowsAuthentication = true
            },
            MasterDataRequest = new InitialMasterDataProvisioningRequest
            {
                OrganizationName = "Test Org",
                CompanyName = "Test Company",
                BranchName = "Test Branch"
            },
            AdminRequest = new FirstAdminProvisioningRequest
            {
                UserName = "admin",
                DisplayName = "Administrator",
                Email = "admin@example.com",
                Password = "Password123!"
            },
            Marker = new CommissioningMarker
            {
                CommissionedAtUtc = DateTimeOffset.UtcNow,
                ProductVersion = "1.0.2",
                MachineId = "TEST-HW",
                DatabaseServer = "localhost",
                DatabaseName = "TestDb",
                OrganizationName = "Test Org",
                CompanyName = "Test Company",
                BranchName = "Test Branch",
                TerminalName = "T-01",
                AdminUserName = "admin"
            }
        };

        // Act
        var result = await coordinator.ExecuteCommissioningAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.True(fakeBootstrap.CreatedCalled);
        Assert.Equal(fakeMasterData.CompanyId, result.CompanyId);
        Assert.Equal(fakeMasterData.BranchId, result.BranchId);
        Assert.Equal(fakeAdmin.AdminId, result.AdminUserId);
    }

    [Fact]
    public async Task ExecuteCommissioningAsync_Fails_WhenMasterDataFails()
    {
        // Arrange
        var fakeBootstrap = new FakeBootstrapFactory();
        var fakeMasterData = new FakeMasterDataProvisioning
        {
            Succeeded = false,
            Message = "Duplicate Company"
        };
        var fakeAdmin = new FakeAdminProvisioning();

        var coordinator = new CommissioningProvisioningCoordinator(
            fakeBootstrap,
            fakeMasterData,
            fakeAdmin);

        var request = new CommissioningExecutionRequest
        {
            ConnectionSettings = new DatabaseConnectionSettings { Server = "localhost", Database = "TestDb", UseWindowsAuthentication = true },
            MasterDataRequest = new InitialMasterDataProvisioningRequest(),
            AdminRequest = new FirstAdminProvisioningRequest(),
            Marker = new CommissioningMarker()
        };

        // Act
        var result = await coordinator.ExecuteCommissioningAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Master data provisioning failed: Duplicate Company", result.Message);
    }
}
