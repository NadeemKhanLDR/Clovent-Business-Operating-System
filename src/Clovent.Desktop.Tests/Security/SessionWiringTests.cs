using Clovent.Authentication.Application;
using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Security;
using Clovent.Authentication.LoginAttempts;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Clovent.Desktop.Login;
using Clovent.Desktop.Security;
using Clovent.Desktop.Sessions;
using Clovent.Desktop.Tests.TestSupport;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Clovent.Platform.Execution;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Security;

public class SessionWiringTests
{
    private sealed class Fixture
    {
        public required ServiceProvider Provider { get; init; }
        public required FakeUserRepository Users { get; init; }
        public required FakeUserCredentialsRepository Credentials { get; init; }
        public required FakeLoginAttemptRepository LoginAttempts { get; init; }
        public required FakeSessionRepository Sessions { get; init; }
        public required FakeRefreshSessionRepository RefreshSessions { get; init; }
        public required FakeIdentityUserService IdentityUserService { get; init; }
        public required CurrentSession CurrentSession { get; init; }
        public required DesktopInteractiveScopeProvider InteractiveScopeProvider { get; init; }
        public required FakeScopeResolver ScopeResolver { get; init; }
        public required LoginService LoginService { get; init; }
    }

    private static Fixture BuildFixture()
    {
        var users = new FakeUserRepository();
        var credentials = new FakeUserCredentialsRepository();
        var loginAttempts = new FakeLoginAttemptRepository();
        var sessions = new FakeSessionRepository();
        var refreshSessions = new FakeRefreshSessionRepository();
        var identityUserService = new FakeIdentityUserService(users);
        var currentSession = new CurrentSession();
        var interactiveScopeProvider = new DesktopInteractiveScopeProvider(currentSession);
        var scopeResolver = new FakeScopeResolver();

        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(Clovent.Authentication.Application.DependencyInjection.ApplicationServiceCollectionExtensions).Assembly));
        services.AddScoped<Clovent.Authentication.Application.Sessions.SessionTerminationCascade>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IUserRepository>(users);
        services.AddSingleton<IUserCredentialsRepository>(credentials);
        services.AddSingleton<ILoginAttemptRepository>(loginAttempts);
        services.AddSingleton<ISessionRepository>(sessions);
        services.AddSingleton<IRefreshSessionRepository>(refreshSessions);
        services.AddSingleton<IIdentityUserService>(identityUserService);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IPinHasher, Pbkdf2PinHasher>();
        services.AddSingleton<ICurrentSession>(currentSession);
        services.AddSingleton<IInteractiveScopeProvider>(interactiveScopeProvider);
        services.AddSingleton<ITrustedScopeResolver>(scopeResolver);

        var provider = services.BuildServiceProvider();
        var loginService = new LoginService(provider.GetRequiredService<IServiceScopeFactory>(), currentSession);

        return new Fixture
        {
            Provider = provider,
            Users = users,
            Credentials = credentials,
            LoginAttempts = loginAttempts,
            Sessions = sessions,
            RefreshSessions = refreshSessions,
            IdentityUserService = identityUserService,
            CurrentSession = currentSession,
            InteractiveScopeProvider = interactiveScopeProvider,
            ScopeResolver = scopeResolver,
            LoginService = loginService,
        };
    }

    private static (User user, UserCredentials credentials) CreateActiveUserWithPassword(
        Fixture fixture,
        string username,
        string password,
        Guid? companyId = null,
        Guid? branchId = null)
    {
        var user = User.Create(Email.Create($"{username}@example.com"), UserName.Create(username), DisplayName.Create(username));
        user.Activate();
        if (companyId.HasValue)
        {
            user.AssignCompany(new Clovent.Identity.Companies.CompanyId(companyId.Value));
        }
        if (branchId.HasValue)
        {
            user.AssignBranch(new Clovent.Identity.Branches.BranchId(branchId.Value));
        }
        fixture.Users.Add(user);

        var hasher = new Pbkdf2PasswordHasher();
        var credentials = UserCredentials.Create(user.Id, DateTimeOffset.UtcNow);
        credentials.SetPassword(PasswordHash.Create(hasher.Hash(password)), DateTimeOffset.UtcNow);
        fixture.Credentials.AddAsync(credentials).GetAwaiter().GetResult();

        return (user, credentials);
    }

    [Fact]
    public async Task DirectBackOfficeLogin_EstablishesRequiredExecutionScope()
    {
        var fixture = BuildFixture();
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var (user, _) = CreateActiveUserWithPassword(fixture, "alice", "Correct123!", companyId, branchId);

        var expectedContext = new PlatformExecutionContext
        {
            UserId = user.Id.Value,
            OrganizationId = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId
        };
        fixture.ScopeResolver.ResultToReturn = ScopeResolutionResult.Success(expectedContext);

        var result = await fixture.LoginService.LoginAsync(new LoginRequest("alice", "Correct123!", null, RememberMe: false));

        Assert.True(result.Succeeded);
        Assert.True(fixture.CurrentSession.IsAuthenticated);

        var activeScope = fixture.InteractiveScopeProvider.GetCurrentContext();
        Assert.NotNull(activeScope);
        Assert.Equal(user.Id.Value, activeScope.UserId);
        Assert.Equal(companyId, activeScope.CompanyId);
        Assert.Equal(branchId, activeScope.BranchId);
    }

    [Fact]
    public async Task LogoutAndUserSwitching_InvokesContextClearing()
    {
        var fixture = BuildFixture();
        var (user, _) = CreateActiveUserWithPassword(fixture, "alice", "Correct123!", Guid.NewGuid(), Guid.NewGuid());

        fixture.ScopeResolver.ResultToReturn = ScopeResolutionResult.Success(new PlatformExecutionContext
        {
            UserId = user.Id.Value,
            OrganizationId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            BranchId = Guid.NewGuid()
        });

        await fixture.LoginService.LoginAsync(new LoginRequest("alice", "Correct123!", null, RememberMe: false));
        Assert.NotNull(fixture.InteractiveScopeProvider.GetCurrentContext());

        // User logs out via LoginService
        await fixture.LoginService.SignOutAsync();

        Assert.False(fixture.CurrentSession.IsAuthenticated);
        Assert.Null(fixture.InteractiveScopeProvider.GetCurrentContext());
    }

    [Fact]
    public async Task CurrentSessionDirectSignOut_AutomaticallyClearsInteractiveScope()
    {
        var fixture = BuildFixture();
        var (user, _) = CreateActiveUserWithPassword(fixture, "alice", "Correct123!", Guid.NewGuid(), Guid.NewGuid());

        fixture.ScopeResolver.ResultToReturn = ScopeResolutionResult.Success(new PlatformExecutionContext
        {
            UserId = user.Id.Value,
            OrganizationId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            BranchId = Guid.NewGuid()
        });

        await fixture.LoginService.LoginAsync(new LoginRequest("alice", "Correct123!", null, RememberMe: false));
        Assert.NotNull(fixture.InteractiveScopeProvider.GetCurrentContext());

        // Direct session sign out (e.g. from user switching or lock screen)
        fixture.CurrentSession.SignOut();

        Assert.False(fixture.CurrentSession.IsAuthenticated);
        Assert.Null(fixture.InteractiveScopeProvider.GetCurrentContext());
    }

    [Fact]
    public async Task FailedLogin_CannotRetainPreviousUsersScope()
    {
        var fixture = BuildFixture();
        var (alice, _) = CreateActiveUserWithPassword(fixture, "alice", "Correct123!", Guid.NewGuid(), Guid.NewGuid());
        CreateActiveUserWithPassword(fixture, "bob", "BobPass123!", Guid.NewGuid(), Guid.NewGuid());

        fixture.ScopeResolver.ResultToReturn = ScopeResolutionResult.Success(new PlatformExecutionContext
        {
            UserId = alice.Id.Value,
            OrganizationId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            BranchId = Guid.NewGuid()
        });

        // Alice logs in
        await fixture.LoginService.LoginAsync(new LoginRequest("alice", "Correct123!", null, RememberMe: false));
        Assert.NotNull(fixture.InteractiveScopeProvider.GetCurrentContext());

        // Bob attempts to log in with WRONG password
        var failResult = await fixture.LoginService.LoginAsync(new LoginRequest("bob", "WrongPassword!", null, RememberMe: false));

        Assert.False(failResult.Succeeded);
        // Alice's scope must NOT be retained
        Assert.Null(fixture.InteractiveScopeProvider.GetCurrentContext());
    }

    [Fact]
    public async Task FailedScopeResolution_TerminatesSessionAndFailsLogin()
    {
        var fixture = BuildFixture();
        var (user, _) = CreateActiveUserWithPassword(fixture, "alice", "Correct123!", Guid.NewGuid(), Guid.NewGuid());

        // Scope resolution fails (e.g. company inactive, unassigned user, or hierarchy mismatch)
        fixture.ScopeResolver.ResultToReturn = ScopeResolutionResult.Failed("Hierarchy inconsistency");

        var result = await fixture.LoginService.LoginAsync(new LoginRequest("alice", "Correct123!", null, RememberMe: false));

        // Authentication might have validated credentials, but login MUST FAIL due to scope resolution failure
        Assert.False(result.Succeeded);
        Assert.Contains("Hierarchy inconsistency", result.ErrorMessage);
        Assert.False(fixture.CurrentSession.IsAuthenticated);
        Assert.Null(fixture.InteractiveScopeProvider.GetCurrentContext());
    }

    private sealed class FakeScopeResolver : ITrustedScopeResolver
    {
        public ScopeResolutionResult ResultToReturn { get; set; } = ScopeResolutionResult.Failed("Not configured");

        public Task<ScopeResolutionResult> ResolveAndValidateScopeAsync(
            ScopeResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ResultToReturn);
        }
    }
}
