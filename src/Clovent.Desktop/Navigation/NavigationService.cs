using Clovent.Desktop.Forms.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Navigation;

/// <summary>
/// EF-free, DevExpress-free <see cref="INavigationService"/> implementation -
/// a plain dictionary of factories over an <see cref="IWorkspaceHost"/>.
/// </summary>
/// <remarks>
/// Resolves <see cref="IWorkspaceHost"/> lazily, from <paramref name="serviceProvider"/>,
/// on first <see cref="NavigateTo"/> call rather than taking it as a direct
/// constructor dependency. <c>IWorkspaceHost</c> is registered as a factory
/// that resolves <c>MainForm</c>, and <c>MainForm</c>'s own constructor
/// depends on <see cref="INavigationService"/> - a real circular dependency
/// (NavigationService → IWorkspaceHost → MainForm → NavigationService) that
/// the container's static cycle detector cannot see through the factory
/// delegate, so it recurses until the stack is exhausted instead of throwing
/// a clean error. Deferring the lookup to first use breaks the cycle: by the
/// time any code actually calls <see cref="NavigateTo"/>, this service's own
/// construction has already completed and is cached, so <c>MainForm</c>'s
/// constructor resolves the already-built singleton instead of re-entering
/// this constructor.
/// </remarks>
public sealed class NavigationService(IServiceProvider serviceProvider, ILogger<NavigationService> logger) : INavigationService
{
    private readonly Dictionary<string, Func<Control>> _factories = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _registrationOrder = [];

    /// <inheritdoc/>
    public string? CurrentKey { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> RegisteredKeys => _registrationOrder.AsReadOnly();

    /// <inheritdoc/>
    public event EventHandler<string>? Navigated;

    /// <inheritdoc/>
    public void Register(string key, Func<Control> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (_factories.ContainsKey(key))
        {
            throw new ArgumentException($"A view is already registered under key '{key}'.", nameof(key));
        }

        _factories[key] = factory;
        _registrationOrder.Add(key);
        logger.LogDebug("Registered navigation view '{Key}'.", key);
    }

    /// <inheritdoc/>
    public void Unregister(string key)
    {
        if (_factories.Remove(key))
        {
            _registrationOrder.RemoveAll(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc/>
    public void NavigateTo(string key, string? caption = null)
    {
        if (!_factories.TryGetValue(key, out var factory))
        {
            throw new KeyNotFoundException($"No view is registered under key '{key}'.");
        }

        serviceProvider.GetRequiredService<IWorkspaceHost>().ShowDocument(key, caption ?? key, factory);
        CurrentKey = key;
        logger.LogInformation("Navigated to '{Key}'.", key);
        Navigated?.Invoke(this, key);
    }
}
