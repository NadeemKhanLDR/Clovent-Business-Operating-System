using Clovent.Authentication.Application;
using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Security;
using Clovent.Authentication.LoginAttempts;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Clovent.Desktop.Login;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Tests.TestSupport;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Login;

public class LoginServiceTests
{
    private sealed class Fixture
    {
        public required ServiceProvider Provider { get; init; }
        public required FakeUserRepository Users { get; init; }
        public required FakeUserCredentialsRepository Credentials { get; init; }
        public required FakeLoginAttemptRepository LoginAttempts { get; init; }
        public required FakeRefreshSessionRepository RefreshSessions { get; init; }
        public required FakeIdentityUserService IdentityUserService { get; init; }
        public required CurrentSession CurrentSession { get; init; }
        public required LoginService LoginService { get; init; }
    }

    private static Fixture BuildFixture()
    {
        var users = new FakeUserRepository();
        var credentials = new FakeUserCredentialsRepository();
        var loginAttempts = new FakeLoginAttemptRepository();
        var refreshSessions = new FakeRefreshSessionRepository();
        var identityUserService = new FakeIdentityUserService(users);
        var currentSession = new CurrentSession();

        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(Clovent.Authentication.Application.DependencyInjection.ApplicationServiceCollectionExtensions).Assembly));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IUserRepository>(users);
        services.AddSingleton<IUserCredentialsRepository>(credentials);
        services.AddSingleton<ILoginAttemptRepository>(loginAttempts);
        services.AddSingleton<ISessionRepository, FakeSessionRepository>();
        services.AddSingleton<IRefreshSessionRepository>(refreshSessions);
        services.AddSingleton<IIdentityUserService>(identityUserService);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IPinHasher, Pbkdf2PinHasher>();

        var provider = services.BuildServiceProvider();
        var loginService = new LoginService(provider.GetRequiredService<IServiceScopeFactory>(), currentSession);

        return new Fixture
        {
            Provider = provider,
            Users = users,
            Credentials = credentials,
            LoginAttempts = loginAttempts,
            RefreshSessions = refreshSessions,
            IdentityUserService = identityUserService,
            CurrentSession = currentSession,
            LoginService = loginService,
        };
    }

    private static (User user, UserCredentials credentials) CreateActiveUserWithPassword(Fixture fixture, string password)
    {
        var user = User.Create(Email.Create("alice@example.com"), UserName.Create("alice"), DisplayName.Create("Alice"));
        user.Activate();
        fixture.Users.Add(user);

        var hasher = new Pbkdf2PasswordHasher();
        var credentials = UserCredentials.Create(user.Id, DateTimeOffset.UtcNow);
        credentials.SetPassword(PasswordHash.Create(hasher.Hash(password)), DateTimeOffset.UtcNow);
        fixture.Credentials.AddAsync(credentials).GetAwaiter().GetResult();

        return (user, credentials);
    }

    [Fact]
    public async Task LoginAsync_CorrectPassword_SucceedsAndEstablishesCurrentSession()
    {
        var fixture = BuildFixture();
        var (user, _) = CreateActiveUserWithPassword(fixture, "Correct123!");

        var result = await fixture.LoginService.LoginAsync(new LoginRequest("alice", "Correct123!", null, RememberMe: false));

        Assert.True(result.Succeeded);
        Assert.True(fixture.CurrentSession.IsAuthenticated);
        Assert.Equal(user.Id.Value, fixture.CurrentSession.UserId);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_FailsWithGenericMessageAndIncrementsFailedAttempts()
    {
        var fixture = BuildFixture();
        CreateActiveUserWithPassword(fixture, "Correct123!");

        var result = await fixture.LoginService.LoginAsync(new LoginRequest("alice", "WrongPassword", null, RememberMe: false));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        Assert.False(fixture.CurrentSession.IsAuthenticated);

        var user = await fixture.Users.GetByUserNameAsync(UserName.Create("alice"));
        var credentials = await fixture.Credentials.GetByUserIdAsync(user!.Id);
        Assert.Equal(1, credentials!.FailedAttempts.Count);
    }

    [Fact]
    public async Task LoginAsync_UnknownUser_FailsAndRecordsUserNotFoundAttempt()
    {
        var fixture = BuildFixture();

        var result = await fixture.LoginService.LoginAsync(new LoginRequest("nobody", "whatever", null, RememberMe: false));

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
        Assert.Contains(fixture.LoginAttempts.All, a => a.Outcome == LoginOutcome.UserNotFound);
    }

    [Fact]
    public async Task LoginAsync_LockedUser_FailsWithLockedMessage()
    {
        var fixture = BuildFixture();
        var (user, _) = CreateActiveUserWithPassword(fixture, "Correct123!");
        user.Lock();

        var result = await fixture.LoginService.LoginAsync(new LoginRequest("alice", "Correct123!", null, RememberMe: false));

        Assert.False(result.Succeeded);
        Assert.Contains("locked", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_RememberMeTrue_IssuesRefreshSession()
    {
        var fixture = BuildFixture();
        CreateActiveUserWithPassword(fixture, "Correct123!");

        await fixture.LoginService.LoginAsync(new LoginRequest("alice", "Correct123!", null, RememberMe: true));

        Assert.Single(fixture.RefreshSessions.All);
    }

    [Fact]
    public async Task LoginAsync_RememberMeFalse_DoesNotIssueRefreshSession()
    {
        var fixture = BuildFixture();
        CreateActiveUserWithPassword(fixture, "Correct123!");

        await fixture.LoginService.LoginAsync(new LoginRequest("alice", "Correct123!", null, RememberMe: false));

        Assert.Empty(fixture.RefreshSessions.All);
    }

    [Fact]
    public async Task LoginAsync_FifthConsecutiveFailure_LocksTheUser()
    {
        var fixture = BuildFixture();
        CreateActiveUserWithPassword(fixture, "Correct123!");

        for (var i = 0; i < 5; i++)
        {
            await fixture.LoginService.LoginAsync(new LoginRequest("alice", "WrongPassword", null, RememberMe: false));
        }

        Assert.Single(fixture.IdentityUserService.LockedUserIds);
    }

    [Fact]
    public async Task LoginAsync_CorrectPin_Succeeds()
    {
        var fixture = BuildFixture();
        var user = User.Create(Email.Create("bob@example.com"), UserName.Create("bob"), DisplayName.Create("Bob"));
        user.Activate();
        fixture.Users.Add(user);

        var pinHasher = new Pbkdf2PinHasher();
        var credentials = UserCredentials.Create(user.Id, DateTimeOffset.UtcNow);
        credentials.SetPin(PinHash.Create(pinHasher.Hash("482913")), DateTimeOffset.UtcNow);
        await fixture.Credentials.AddAsync(credentials);

        var result = await fixture.LoginService.LoginAsync(new LoginRequest("bob", null, "482913", RememberMe: false));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task LoginAsync_NeitherPasswordNorPin_FailsValidationBeforeLookingUpUser()
    {
        var fixture = BuildFixture();

        var result = await fixture.LoginService.LoginAsync(new LoginRequest("alice", null, null, RememberMe: false));

        Assert.False(result.Succeeded);
        Assert.Empty(fixture.LoginAttempts.All);
    }
}
