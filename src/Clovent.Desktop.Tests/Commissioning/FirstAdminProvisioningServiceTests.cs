using Clovent.Authentication.Application;
using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.Infrastructure.Security;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class FirstAdminProvisioningServiceTests : IDisposable
{
    private readonly SqliteConnection _identityConn;
    private readonly SqliteConnection _authConn;
    private readonly ServiceProvider _serviceProvider;
    private readonly IdentityDbContext _identityDbContext;
    private readonly AuthenticationDbContext _authDbContext;
    private readonly FirstAdminProvisioningService _sut;

    public FirstAdminProvisioningServiceTests()
    {
        _identityConn = new SqliteConnection("DataSource=:memory:");
        _identityConn.Open();
        _authConn = new SqliteConnection("DataSource=:memory:");
        _authConn.Open();

        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_identityConn)
            .Options;

        var authOptions = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseSqlite(_authConn)
            .Options;

        _identityDbContext = new IdentityDbContext(identityOptions);
        _identityDbContext.Database.EnsureCreated();

        _authDbContext = new AuthenticationDbContext(authOptions);
        _authDbContext.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped(_ => new IdentityDbContext(identityOptions));
        services.AddScoped(_ => new AuthenticationDbContext(authOptions));

        _serviceProvider = services.BuildServiceProvider();
        _sut = new FirstAdminProvisioningService();
    }

    public void Dispose()
    {
        _identityDbContext.Dispose();
        _authDbContext.Dispose();
        _identityConn.Dispose();
        _authConn.Dispose();
        _serviceProvider.Dispose();
    }

    [Theory]
    [InlineData("", "Password is required.")]
    [InlineData("Ab1!", "Password must be at least 8 characters long.")]
    [InlineData("password123!", "Password must contain at least one uppercase letter.")]
    [InlineData("PASSWORD123!", "Password must contain at least one lowercase letter.")]
    [InlineData("Password!", "Password must contain at least one digit.")]
    [InlineData("Password123", "Password must contain at least one special character.")]
    [InlineData("Admin123!", "Prohibited password: 'Admin123!' is a known insecure default.")]
    public void ValidatePasswordPolicy_RejectsInsecurePasswords(string password, string expectedErrorSubstr)
    {
        var error = FirstAdminProvisioningService.ValidatePasswordPolicy(password, password, "testadmin");
        Assert.NotNull(error);
        Assert.Contains(expectedErrorSubstr, error);
    }

    [Fact]
    public void ValidatePasswordPolicy_RejectsPasswordIdenticalToUsername()
    {
        var error = FirstAdminProvisioningService.ValidatePasswordPolicy("AdminMaster1!", "AdminMaster1!", "AdminMaster1!");
        Assert.NotNull(error);
        Assert.Contains("Password cannot be identical to the username.", error);
    }

    [Fact]
    public void ValidatePasswordPolicy_RejectsMismatchedConfirmPassword()
    {
        var error = FirstAdminProvisioningService.ValidatePasswordPolicy("SecureP@ssw0rd1", "DifferentP@ssw0rd2", "sysadmin");
        Assert.NotNull(error);
        Assert.Contains("Passwords do not match.", error);
    }

    [Fact]
    public void ValidatePasswordPolicy_AcceptsValidEnterprisePassword()
    {
        var error = FirstAdminProvisioningService.ValidatePasswordPolicy("VeryS3cur3!P@ss", "VeryS3cur3!P@ss", "sysadmin");
        Assert.Null(error);
    }

    [Fact]
    public async Task CanProvisionFirstAdminAsync_ReturnsTrue_WhenNoUsersExist()
    {
        var canProvision = await _sut.CanProvisionFirstAdminAsync(_identityDbContext);
        Assert.True(canProvision);
    }

    [Fact]
    public async Task CanProvisionFirstAdminAsync_ReturnsFalse_WhenActiveAdminExists()
    {
        // Arrange: Create Administrator role and active user with that role
        var role = Role.Create(RoleName.Create("Administrator"));
        await _identityDbContext.Roles.AddAsync(role);

        var user = User.Create(
            Email.Create("admin@company.com"),
            UserName.Create("rootadmin"),
            DisplayName.Create("Root Admin"));
        user.Activate();
        user.AssignRole(role.Id);

        await _identityDbContext.Users.AddAsync(user);
        await _identityDbContext.SaveChangesAsync();

        // Act
        var canProvision = await _sut.CanProvisionFirstAdminAsync(_identityDbContext);

        // Assert
        Assert.False(canProvision);
    }

    [Fact]
    public async Task ProvisionFirstAdminAsync_Succeeds_AndEnforcesOneTimeBootstrapRule()
    {
        // Arrange
        var request = new FirstAdminProvisioningRequest
        {
            UserName = "masteradmin",
            DisplayName = "System Administrator",
            Email = "admin@enterprise.local",
            Password = "Pr0visi0n!P@ss2026",
            ConfirmPassword = "Pr0visi0n!P@ss2026"
        };

        // Act 1: First provisioning attempt should succeed
        var (success, message, userId) = await _sut.ProvisionFirstAdminAsync(_serviceProvider, request);

        // Assert 1
        Assert.True(success);
        Assert.NotNull(userId);
        Assert.Contains("successfully provisioned", message);

        // Verify entities in database
        using (var scope = _serviceProvider.CreateScope())
        {
            var idDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var authDb = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();

            var createdUser = await idDb.Users.FirstOrDefaultAsync(u => u.UserName == UserName.Create("masteradmin"));
            Assert.NotNull(createdUser);
            Assert.Equal(UserStatus.Active, createdUser.Status);
            Assert.NotEmpty(createdUser.RoleIds);

            var createdCreds = await authDb.UserCredentials.FirstOrDefaultAsync(c => c.UserId == createdUser.Id);
            Assert.NotNull(createdCreds);
            Assert.NotNull(createdCreds.PasswordHash);
        }

        // Act 2: Second provisioning attempt must IMMEDIATELY fail with the exact bootstrap protection error
        var secondRequest = new FirstAdminProvisioningRequest
        {
            UserName = "backdooradmin",
            DisplayName = "Backdoor Admin",
            Email = "backdoor@enterprise.local",
            Password = "Diff3r3nt!S3cur3P@ss",
            ConfirmPassword = "Diff3r3nt!S3cur3P@ss"
        };

        var (success2, message2, userId2) = await _sut.ProvisionFirstAdminAsync(_serviceProvider, secondRequest);

        // Assert 2
        Assert.False(success2);
        Assert.Null(userId2);
        Assert.Equal("Administrator account already exists. Self-provisioning is disabled.", message2);
    }
}
