using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Tests.TestSupport;
using Xunit;

namespace Clovent.Identity.Application.Tests.Authorization;

public class ModuleAuthorizationPolicyTests
{
    [Fact]
    public async Task CanAccessModuleAsync_ChecksModulePrefixedPermissionCode()
    {
        var recording = new RecordingAuthorizationService();
        var policy = new ModuleAuthorizationPolicy(recording);

        await policy.CanAccessModuleAsync(Guid.NewGuid(), "RestaurantPOS");

        Assert.Equal("module.restaurantpos", recording.LastCheckedPermissionCode);
    }

    [Fact]
    public async Task CanAccessModuleAsync_ReturnsUnderlyingResult()
    {
        var recording = new RecordingAuthorizationService { Result = false };
        var policy = new ModuleAuthorizationPolicy(recording);

        Assert.False(await policy.CanAccessModuleAsync(Guid.NewGuid(), "RestaurantPOS"));
    }
}

public class MenuAuthorizationPolicyTests
{
    [Fact]
    public async Task CanViewMenuItemAsync_ChecksMenuPrefixedPermissionCode()
    {
        var recording = new RecordingAuthorizationService();
        var policy = new MenuAuthorizationPolicy(recording);

        await policy.CanViewMenuItemAsync(Guid.NewGuid(), "Dashboard");

        Assert.Equal("menu.dashboard", recording.LastCheckedPermissionCode);
    }
}

public class FeatureAuthorizationPolicyTests
{
    [Fact]
    public async Task CanUseFeatureAsync_ChecksFeaturePrefixedPermissionCode()
    {
        var recording = new RecordingAuthorizationService();
        var policy = new FeatureAuthorizationPolicy(recording);

        await policy.CanUseFeatureAsync(Guid.NewGuid(), "ExportExcel");

        Assert.Equal("feature.exportexcel", recording.LastCheckedPermissionCode);
    }
}
