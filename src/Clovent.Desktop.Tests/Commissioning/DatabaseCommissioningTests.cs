using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Configuration;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class DatabaseCommissioningTests
{
    [Fact]
    public void DatabaseErrorMasker_MasksPlainTextPassword_WhenPresentInMessage()
    {
        // Arrange
        const string rawMessage = "Failed to connect using user 'sa' and password 'SuperSecret123!'.";
        const string secretPassword = "SuperSecret123!";

        // Act
        var result = DatabaseErrorMasker.Mask(rawMessage, secretPassword);

        // Assert
        Assert.DoesNotContain(secretPassword, result);
        Assert.Contains("******", result);
    }

    [Fact]
    public void DatabaseErrorMasker_MasksConnectionStringPasswords()
    {
        // Arrange
        const string cs = "Server=localhost;Database=TestDb;User Id=dbadmin;Password=UltraClassified!2026;TrustServerCertificate=True;";
        const string rawMessage = $"Connection failed: {cs}";

        // Act
        var result = DatabaseErrorMasker.Mask(rawMessage, cs);

        // Assert
        Assert.DoesNotContain("UltraClassified!2026", result);
        Assert.Contains("Password=******", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DatabaseErrorMasker_HandlesNullAndEmptyInput()
    {
        Assert.Equal(string.Empty, DatabaseErrorMasker.Mask(null));
        Assert.Equal(string.Empty, DatabaseErrorMasker.Mask(string.Empty));
        Assert.Equal(string.Empty, DatabaseErrorMasker.Mask("   "));
    }

    [Fact]
    public async Task DatabaseSchemaCompatibilityValidator_ReturnsConnectionFailed_OnEmptyConnectionString()
    {
        // Arrange
        var validator = new DatabaseSchemaCompatibilityValidator();

        // Act
        var result = await validator.ValidateCompatibilityAsync(string.Empty);

        // Assert
        Assert.Equal(SchemaCompatibilityStatus.ConnectionFailed, result.Status);
        Assert.False(result.IsCompatible);
        Assert.Contains("No database connection string", result.Message);
    }

    [Fact]
    public async Task DatabaseSchemaCompatibilityValidator_ReturnsConnectionFailed_WhenServerUnreachable()
    {
        // Arrange
        var validator = new DatabaseSchemaCompatibilityValidator();
        const string invalidCs = "Server=127.0.0.1,65534;Database=NonExistentDb_12345;Connection Timeout=1;TrustServerCertificate=True;";

        // Act
        var result = await validator.ValidateCompatibilityAsync(invalidCs);

        // Assert
        Assert.Equal(SchemaCompatibilityStatus.ConnectionFailed, result.Status);
        Assert.False(result.IsCompatible);
        Assert.NotNull(result.Message);
        Assert.DoesNotContain("Password", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SchemaCompatibilityResult_IsCompatible_ReflectsStatusCorrectly()
    {
        var compatible = new SchemaCompatibilityResult(SchemaCompatibilityStatus.Compatible, "Compatible");
        var tooOld = new SchemaCompatibilityResult(SchemaCompatibilityStatus.DatabaseTooOld, "Too Old");
        var newer = new SchemaCompatibilityResult(SchemaCompatibilityStatus.DatabaseNewer, "Newer");
        var failed = new SchemaCompatibilityResult(SchemaCompatibilityStatus.ConnectionFailed, "Failed");

        Assert.True(compatible.IsCompatible);
        Assert.False(tooOld.IsCompatible);
        Assert.False(newer.IsCompatible);
        Assert.False(failed.IsCompatible);
    }

    [Fact]
    public async Task DatabaseBackupService_ReturnsError_WhenConnectionStringEmpty()
    {
        // Arrange
        var backupService = new DatabaseBackupService();

        // Act
        var (success, backupPath, message) = await backupService.CreateBackupAsync(string.Empty);

        // Assert
        Assert.False(success);
        Assert.Equal(string.Empty, backupPath);
        Assert.Contains("empty", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DatabaseBackupService_FailsSafely_WhenTargetDirectoryInvalid()
    {
        // Arrange
        var backupService = new DatabaseBackupService();
        const string invalidCs = "Server=127.0.0.1,65534;Database=TestDb;Connection Timeout=1;TrustServerCertificate=True;";

        // Act
        var (success, backupPath, message) = await backupService.CreateBackupAsync(invalidCs, "Z:\\NonExistentDrive_XYZ123\\Backups");

        // Assert
        Assert.False(success);
        Assert.Equal(string.Empty, backupPath);
        Assert.NotNull(message);
    }

    [Fact]
    public async Task DatabaseProvisioningService_ThrowsArgumentNullException_OnNullSettings()
    {
        var service = new DatabaseProvisioningService();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.TestConnectionAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.DatabaseExistsAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateDatabaseAsync(null!));
    }

    [Fact]
    public async Task DatabaseProvisioningService_TestConnectionAsync_MasksPasswordOnFailure()
    {
        // Arrange
        var service = new DatabaseProvisioningService();
        var settings = new DatabaseConnectionSettings
        {
            Server = "127.0.0.1,65534",
            Database = "Clovent_BusinessOperatingSystem",
            UseWindowsAuthentication = false,
            UserId = "testuser",
            PlainTextPassword = "SecretPlainPassword123!",
            ConnectionTimeout = 1
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(settings);

        // Assert
        Assert.False(success);
        Assert.DoesNotContain("SecretPlainPassword123!", message);
    }

    [Fact]
    public async Task DatabaseProvisioningService_ApplyMigrationsAsync_ThrowsOnEmptyConnectionString()
    {
        var service = new DatabaseProvisioningService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ApplyMigrationsAsync(string.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ApplyMigrationsAsync("   "));
    }
}
