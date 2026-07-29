using Clovent.Identity.Application.Authorization;
using Xunit;

namespace Clovent.Identity.Application.Tests.Authorization;

public class AuthorizationPolicyProviderTests
{
    [Fact]
    public void AddPolicy_ThenGetPolicy_ReturnsIt()
    {
        var provider = new AuthorizationPolicyProvider();
        var policy = new AuthorizationPolicy("CanRunReports", ["module.reporting"]);

        provider.AddPolicy(policy);

        Assert.Same(policy, provider.GetPolicy("CanRunReports"));
    }

    [Fact]
    public void GetPolicy_IsCaseInsensitive()
    {
        var provider = new AuthorizationPolicyProvider();
        provider.AddPolicy(new AuthorizationPolicy("CanRunReports", ["module.reporting"]));

        Assert.NotNull(provider.GetPolicy("canrunreports"));
    }

    [Fact]
    public void GetPolicy_Unregistered_ReturnsNull()
    {
        var provider = new AuthorizationPolicyProvider();

        Assert.Null(provider.GetPolicy("Missing"));
    }

    [Fact]
    public void AddPolicy_SameName_ReplacesPrevious()
    {
        var provider = new AuthorizationPolicyProvider();
        provider.AddPolicy(new AuthorizationPolicy("P", ["a"]));
        var replacement = new AuthorizationPolicy("P", ["b", "c"]);

        provider.AddPolicy(replacement);

        Assert.Same(replacement, provider.GetPolicy("P"));
    }
}
