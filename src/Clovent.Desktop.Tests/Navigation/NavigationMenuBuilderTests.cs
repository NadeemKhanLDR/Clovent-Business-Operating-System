using Clovent.Desktop.Navigation;
using Clovent.Desktop.Shell;
using Clovent.Identity.Application.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Navigation;

public class NavigationMenuBuilderTests
{
    private sealed class FakeWorkspaceHost : IWorkspaceHost
    {
        public void SetContent(Control content)
        {
        }
    }

    private sealed class FakeMenuAuthorizationPolicy(HashSet<string> allowedKeys) : IMenuAuthorizationPolicy
    {
        public Task<bool> CanViewMenuItemAsync(Guid userId, string menuItemCode, CancellationToken cancellationToken = default) =>
            Task.FromResult(allowedKeys.Contains(menuItemCode));
    }

    private static NavigationService CreateNavigationService()
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<NavigationService>.Instance;

        var services = new ServiceCollection();
        services.AddSingleton<IWorkspaceHost>(new FakeWorkspaceHost());
        var provider = services.BuildServiceProvider();

        return new NavigationService(provider, logger);
    }

    [Fact]
    public async Task GetVisibleMenuKeysAsync_OnlyReturnsPermittedKeys()
    {
        var navigationService = CreateNavigationService();
        navigationService.Register("dashboard", () => new Control());
        navigationService.Register("admin", () => new Control());
        var policy = new FakeMenuAuthorizationPolicy(["dashboard"]);
        var builder = new NavigationMenuBuilder(navigationService, policy);

        var visible = await builder.GetVisibleMenuKeysAsync(Guid.NewGuid());

        Assert.Equal(["dashboard"], visible);
    }

    [Fact]
    public async Task GetVisibleMenuKeysAsync_NoPermittedKeys_ReturnsEmpty()
    {
        var navigationService = CreateNavigationService();
        navigationService.Register("admin", () => new Control());
        var policy = new FakeMenuAuthorizationPolicy([]);
        var builder = new NavigationMenuBuilder(navigationService, policy);

        var visible = await builder.GetVisibleMenuKeysAsync(Guid.NewGuid());

        Assert.Empty(visible);
    }

    [Fact]
    public async Task GetVisibleMenuKeysAsync_PreservesRegistrationOrder()
    {
        var navigationService = CreateNavigationService();
        navigationService.Register("a", () => new Control());
        navigationService.Register("b", () => new Control());
        navigationService.Register("c", () => new Control());
        var policy = new FakeMenuAuthorizationPolicy(["c", "a", "b"]);
        var builder = new NavigationMenuBuilder(navigationService, policy);

        var visible = await builder.GetVisibleMenuKeysAsync(Guid.NewGuid());

        Assert.Equal(["a", "b", "c"], visible);
    }
}
