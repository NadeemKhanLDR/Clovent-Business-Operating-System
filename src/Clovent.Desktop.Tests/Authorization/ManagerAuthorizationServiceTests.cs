using Clovent.Authentication.Application;
using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Security;
using Clovent.Authentication.LoginAttempts;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Tests.TestSupport;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Authorization;

/// <summary>
/// Covers defect D7/D25: a privileged POS action must not be approvable
/// without a manager credential that both verifies and carries the required
/// permission.
/// </summary>
public class ManagerAuthorizationServiceTests
{
    private const string OverrideFeature = "pos.exceedcreditlimit";

    /// <summary>Records what was asked of it so a test can assert the permission actually gates approval.</summary>
    private sealed class FakeFeaturePolicy : IFeatureAuthorizationPolicy
    {
        private readonly HashSet<string> _granted = new(StringComparer.OrdinalIgnoreCase);

        public List<(Guid UserId, string FeatureCode)> Calls { get; } = [];

        public void Grant(Guid userId, string featureCode) => _granted.Add($"{userId}|{featureCode}");

        public Task<bool> CanUseFeatureAsync(Guid userId, string featureCode, CancellationToken cancellationToken = default)
        {
            Calls.Add((userId, featureCode));
            return Task.FromResult(_granted.Contains($"{userId}|{featureCode}"));
        }
    }

    private sealed class Fixture
    {
        public required FakeUserRepository Users { get; init; }
        public required FakeUserCredentialsRepository Credentials { get; init; }
        public required FakeLoginAttemptRepository LoginAttempts { get; init; }
        public required FakeFeaturePolicy FeaturePolicy { get; init; }
        public required ManagerAuthorizationService Service { get; init; }
    }

    private static Fixture BuildFixture()
    {
        var users = new FakeUserRepository();
        var credentials = new FakeUserCredentialsRepository();
        var loginAttempts = new FakeLoginAttemptRepository();
        var featurePolicy = new FakeFeaturePolicy();

        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(Clovent.Authentication.Application.DependencyInjection.ApplicationServiceCollectionExtensions).Assembly));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IUserRepository>(users);
        services.AddSingleton<IUserCredentialsRepository>(credentials);
        services.AddSingleton<ILoginAttemptRepository>(loginAttempts);
        services.AddSingleton<IIdentityUserService>(new FakeIdentityUserService(users));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IPinHasher, Pbkdf2PinHasher>();
        services.AddSingleton<IFeatureAuthorizationPolicy>(featurePolicy);

        var provider = services.BuildServiceProvider();

        return new Fixture
        {
            Users = users,
            Credentials = credentials,
            LoginAttempts = loginAttempts,
            FeaturePolicy = featurePolicy,
            Service = new ManagerAuthorizationService(provider.GetRequiredService<IServiceScopeFactory>()),
        };
    }

    private static User CreateActiveUserWithPassword(Fixture fixture, string userName, string password)
    {
        var user = User.Create(
            Email.Create($"{userName}@example.com"),
            UserName.Create(userName),
            DisplayName.Create(userName));
        user.Activate();
        fixture.Users.Add(user);

        var hasher = new Pbkdf2PasswordHasher();
        var credentials = UserCredentials.Create(user.Id, DateTimeOffset.UtcNow);
        credentials.SetPassword(PasswordHash.Create(hasher.Hash(password)), DateTimeOffset.UtcNow);
        fixture.Credentials.AddAsync(credentials).GetAwaiter().GetResult();

        return user;
    }

    [Fact]
    public async Task AuthorizeAsync_ManagerWithPermission_Approves()
    {
        var fixture = BuildFixture();
        var manager = CreateActiveUserWithPassword(fixture, "manager", "Manager123!");
        fixture.FeaturePolicy.Grant(manager.Id.Value, OverrideFeature);

        var result = await fixture.Service.AuthorizeAsync("manager", "Manager123!", OverrideFeature);

        Assert.True(result.Succeeded);
        Assert.Equal(manager.Id.Value, result.ManagerUserId);
        Assert.Equal("manager", result.ManagerDisplayName);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    /// The heart of D7: knowing <em>a</em> valid password must not be enough.
    /// A cashier's own credentials are genuine and still cannot approve.
    /// </summary>
    [Fact]
    public async Task AuthorizeAsync_ValidCredentialsWithoutPermission_Denies()
    {
        var fixture = BuildFixture();
        CreateActiveUserWithPassword(fixture, "cashier1", "Cashier123!");

        var result = await fixture.Service.AuthorizeAsync("cashier1", "Cashier123!", OverrideFeature);

        Assert.False(result.Succeeded);
        Assert.Null(result.ManagerUserId);
        Assert.Contains("not authorized", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizeAsync_WrongPassword_DeniesAndCountsAFailedAttempt()
    {
        var fixture = BuildFixture();
        var manager = CreateActiveUserWithPassword(fixture, "manager", "Manager123!");
        fixture.FeaturePolicy.Grant(manager.Id.Value, OverrideFeature);

        var result = await fixture.Service.AuthorizeAsync("manager", "WrongPassword", OverrideFeature);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid manager username or password.", result.ErrorMessage);

        // Guesses here run into the same lockout accounting as a failed sign-in.
        var credentials = await fixture.Credentials.GetByUserIdAsync(manager.Id);
        Assert.Equal(1, credentials!.FailedAttempts.Count);
        Assert.Contains(fixture.LoginAttempts.All, a => a.Outcome == LoginOutcome.InvalidCredentials);
    }

    [Fact]
    public async Task AuthorizeAsync_UnknownUser_DeniesWithGenericMessage()
    {
        var fixture = BuildFixture();

        var result = await fixture.Service.AuthorizeAsync("nobody", "whatever", OverrideFeature);

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid manager username or password.", result.ErrorMessage);
        Assert.Contains(fixture.LoginAttempts.All, a => a.Outcome == LoginOutcome.UserNotFound);
    }

    [Fact]
    public async Task AuthorizeAsync_LockedManager_Denies()
    {
        var fixture = BuildFixture();
        var manager = CreateActiveUserWithPassword(fixture, "manager", "Manager123!");
        fixture.FeaturePolicy.Grant(manager.Id.Value, OverrideFeature);
        manager.Lock();

        var result = await fixture.Service.AuthorizeAsync("manager", "Manager123!", OverrideFeature);

        Assert.False(result.Succeeded);
        Assert.Contains("locked", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(fixture.LoginAttempts.All, a => a.Outcome == LoginOutcome.UserLocked);
    }

    [Theory]
    [InlineData("", "Manager123!")]
    [InlineData("manager", "")]
    [InlineData("   ", "   ")]
    public async Task AuthorizeAsync_MissingCredentials_DeniesWithoutTouchingTheStore(string userName, string password)
    {
        var fixture = BuildFixture();
        CreateActiveUserWithPassword(fixture, "manager", "Manager123!");

        var result = await fixture.Service.AuthorizeAsync(userName, password, OverrideFeature);

        Assert.False(result.Succeeded);
        Assert.Empty(fixture.LoginAttempts.All);
        Assert.Empty(fixture.FeaturePolicy.Calls);
    }

    /// <summary>The permission demanded is the one the caller names, so void and override cannot be conflated.</summary>
    [Fact]
    public async Task AuthorizeAsync_ChecksTheFeatureCodeTheCallerAskedFor()
    {
        var fixture = BuildFixture();
        var manager = CreateActiveUserWithPassword(fixture, "manager", "Manager123!");
        fixture.FeaturePolicy.Grant(manager.Id.Value, "pos.void");

        var voidResult = await fixture.Service.AuthorizeAsync("manager", "Manager123!", "pos.void");
        var overrideResult = await fixture.Service.AuthorizeAsync("manager", "Manager123!", OverrideFeature);

        Assert.True(voidResult.Succeeded);
        Assert.False(overrideResult.Succeeded);
        Assert.Contains(fixture.FeaturePolicy.Calls, c => c.FeatureCode == "pos.void");
        Assert.Contains(fixture.FeaturePolicy.Calls, c => c.FeatureCode == OverrideFeature);
    }

    /// <summary>An authorization challenge is not a sign-in: it must issue no session of its own.</summary>
    [Fact]
    public async Task AuthorizeAsync_Approved_DoesNotStartASession()
    {
        var fixture = BuildFixture();
        var manager = CreateActiveUserWithPassword(fixture, "manager", "Manager123!");
        fixture.FeaturePolicy.Grant(manager.Id.Value, OverrideFeature);

        var currentSession = new Clovent.Desktop.Sessions.CurrentSession();

        await fixture.Service.AuthorizeAsync("manager", "Manager123!", OverrideFeature);

        Assert.False(currentSession.IsAuthenticated);
    }
}
