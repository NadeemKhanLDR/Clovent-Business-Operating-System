namespace Clovent.Identity.Application.Authorization;

/// <summary>Registry of named <see cref="AuthorizationPolicy"/> instances a module can register at startup and <see cref="IAuthorizationService.SatisfiesPolicyAsync"/> can evaluate by name.</summary>
public interface IAuthorizationPolicyProvider
{
    /// <summary>Registers a policy, replacing any existing policy with the same <see cref="AuthorizationPolicy.Name"/>.</summary>
    void AddPolicy(AuthorizationPolicy policy);

    /// <summary>Retrieves a registered policy by name, or <see langword="null"/> if none is registered under that name.</summary>
    AuthorizationPolicy? GetPolicy(string name);
}
