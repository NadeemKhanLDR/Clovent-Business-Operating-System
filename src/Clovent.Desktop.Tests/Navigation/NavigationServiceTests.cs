using Clovent.Desktop.Forms.Shell;
using Clovent.Desktop.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Navigation;

public class NavigationServiceTests
{
    private sealed class FakeWorkspaceHost : IWorkspaceHost
    {
        public Control? LastContent { get; private set; }
        public int ShowDocumentCallCount { get; private set; }

        public void ShowDocument(string key, string caption, Func<Control> contentFactory, bool allowMultipleInstances = false)
        {
            LastContent = contentFactory();
            ShowDocumentCallCount++;
        }

        public void SetStatus(string text)
        {
        }
    }

    private static NavigationService CreateService(out FakeWorkspaceHost host)
    {
        host = new FakeWorkspaceHost();

        var services = new ServiceCollection();
        services.AddSingleton<IWorkspaceHost>(host);
        var provider = services.BuildServiceProvider();

        return new NavigationService(provider, NullLogger<NavigationService>.Instance);
    }

    [Fact]
    public void Register_ThenNavigateTo_SetsContentAndCurrentKey()
    {
        var service = CreateService(out var host);
        var control = new Control();
        service.Register("home", () => control);

        service.NavigateTo("home");

        Assert.Same(control, host.LastContent);
        Assert.Equal("home", service.CurrentKey);
    }

    [Fact]
    public void NavigateTo_CallsFactoryFreshEachTime()
    {
        var service = CreateService(out var host);
        var callCount = 0;
        service.Register("home", () => { callCount++; return new Control(); });

        service.NavigateTo("home");
        service.NavigateTo("home");

        Assert.Equal(2, callCount);
        Assert.Equal(2, host.ShowDocumentCallCount);
    }

    [Fact]
    public void Register_DuplicateKey_Throws()
    {
        var service = CreateService(out _);
        service.Register("home", () => new Control());

        Assert.Throws<ArgumentException>(() => service.Register("home", () => new Control()));
    }

    [Fact]
    public void Register_DuplicateKey_IsCaseInsensitive()
    {
        var service = CreateService(out _);
        service.Register("home", () => new Control());

        Assert.Throws<ArgumentException>(() => service.Register("HOME", () => new Control()));
    }

    [Fact]
    public void NavigateTo_UnregisteredKey_Throws()
    {
        var service = CreateService(out _);

        Assert.Throws<KeyNotFoundException>(() => service.NavigateTo("missing"));
    }

    [Fact]
    public void NavigateTo_RaisesNavigatedEventWithKey()
    {
        var service = CreateService(out _);
        service.Register("home", () => new Control());
        string? raisedKey = null;
        service.Navigated += (_, key) => raisedKey = key;

        service.NavigateTo("home");

        Assert.Equal("home", raisedKey);
    }

    [Fact]
    public void Unregister_RemovesKeyFromRegisteredKeys()
    {
        var service = CreateService(out _);
        service.Register("home", () => new Control());

        service.Unregister("home");

        Assert.Empty(service.RegisteredKeys);
        Assert.Throws<KeyNotFoundException>(() => service.NavigateTo("home"));
    }

    [Fact]
    public void Unregister_UnknownKey_DoesNotThrow()
    {
        var service = CreateService(out _);

        service.Unregister("missing");
    }

    [Fact]
    public void RegisteredKeys_PreservesRegistrationOrder()
    {
        var service = CreateService(out _);
        service.Register("a", () => new Control());
        service.Register("b", () => new Control());
        service.Register("c", () => new Control());

        Assert.Equal(["a", "b", "c"], service.RegisteredKeys);
    }
}
