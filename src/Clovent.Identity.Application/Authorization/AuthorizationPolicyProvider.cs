using System.Collections.Concurrent;

namespace Clovent.Identity.Application.Authorization;

/// <summary>In-memory <see cref="IAuthorizationPolicyProvider"/> - a process-wide singleton registry, no persistence needed since policies are code-defined at startup, not user-editable data.</summary>
public sealed class AuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _policies = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public void AddPolicy(AuthorizationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        _policies[policy.Name] = policy;
    }

    /// <inheritdoc/>
    public AuthorizationPolicy? GetPolicy(string name) =>
        _policies.GetValueOrDefault(name);
}
