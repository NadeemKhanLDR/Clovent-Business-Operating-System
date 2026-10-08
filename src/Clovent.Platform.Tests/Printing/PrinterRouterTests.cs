using Clovent.Platform.Printing;
using Xunit;

namespace Clovent.Platform.Tests.Printing;

public class PrinterRouterTests
{
    [Fact]
    public void ResolvePrinter_TerminalLevelMatch_TakesPrecedenceOverBranchAndOrg()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var terminalId = Guid.NewGuid();

        var terminalProfile = new PrinterProfile { Id = Guid.NewGuid(), ProfileName = "Terminal Receipt", Role = PrinterRole.Receipt, IsEnabled = true };
        var branchProfile = new PrinterProfile { Id = Guid.NewGuid(), ProfileName = "Branch Receipt", Role = PrinterRole.Receipt, IsEnabled = true };
        var orgProfile = new PrinterProfile { Id = Guid.NewGuid(), ProfileName = "Org Receipt", Role = PrinterRole.Receipt, IsEnabled = true };

        var config = new PrinterConfiguration
        {
            Profiles = [terminalProfile, branchProfile, orgProfile],
            Assignments =
            [
                new PrinterAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, BranchId = branchId, TerminalId = terminalId, Role = PrinterRole.Receipt, PrinterProfileId = terminalProfile.Id },
                new PrinterAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, BranchId = branchId, Role = PrinterRole.Receipt, PrinterProfileId = branchProfile.Id },
                new PrinterAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, Role = PrinterRole.Receipt, PrinterProfileId = orgProfile.Id }
            ]
        };

        var router = new PrinterRouter(config);

        // Act
        var resolved = router.ResolvePrinter(orgId, branchId, terminalId, PrinterRole.Receipt);

        // Assert
        Assert.NotNull(resolved);
        Assert.Equal(terminalProfile.Id, resolved.Id);
    }

    [Fact]
    public void ResolvePrinter_BranchLevelMatch_TakesPrecedenceWhenTerminalNotAssigned()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var otherTerminalId = Guid.NewGuid();

        var branchProfile = new PrinterProfile { Id = Guid.NewGuid(), ProfileName = "Branch Receipt", Role = PrinterRole.Receipt, IsEnabled = true };
        var orgProfile = new PrinterProfile { Id = Guid.NewGuid(), ProfileName = "Org Receipt", Role = PrinterRole.Receipt, IsEnabled = true };

        var config = new PrinterConfiguration
        {
            Profiles = [branchProfile, orgProfile],
            Assignments =
            [
                new PrinterAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, BranchId = branchId, Role = PrinterRole.Receipt, PrinterProfileId = branchProfile.Id },
                new PrinterAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, Role = PrinterRole.Receipt, PrinterProfileId = orgProfile.Id }
            ]
        };

        var router = new PrinterRouter(config);

        // Act
        var resolved = router.ResolvePrinter(orgId, branchId, otherTerminalId, PrinterRole.Receipt);

        // Assert
        Assert.NotNull(resolved);
        Assert.Equal(branchProfile.Id, resolved.Id);
    }

    [Fact]
    public void ResolvePrinter_NegativeIsolation_NeverResolvesProfileAssignedToDifferentOrganization()
    {
        // Arrange: Org Alpha vs Org Beta
        var orgAlphaId = Guid.NewGuid();
        var orgBetaId = Guid.NewGuid();

        var alphaProfile = new PrinterProfile { Id = Guid.NewGuid(), ProfileName = "Alpha Receipt", Role = PrinterRole.Receipt, IsEnabled = true };
        var betaProfile = new PrinterProfile { Id = Guid.NewGuid(), ProfileName = "Beta Kitchen", Role = PrinterRole.Kitchen, IsEnabled = true };

        var config = new PrinterConfiguration
        {
            Profiles = [alphaProfile, betaProfile],
            Assignments =
            [
                new PrinterAssignment { Id = Guid.NewGuid(), OrganizationId = orgAlphaId, Role = PrinterRole.Receipt, PrinterProfileId = alphaProfile.Id },
                new PrinterAssignment { Id = Guid.NewGuid(), OrganizationId = orgBetaId, Role = PrinterRole.Kitchen, PrinterProfileId = betaProfile.Id }
            ]
        };

        var router = new PrinterRouter(config);

        // Act: Org Alpha requests a Kitchen printer
        var resolved = router.ResolvePrinter(orgAlphaId, branchId: null, terminalId: null, role: PrinterRole.Kitchen);

        // Assert: Must NOT return Beta's printer!
        Assert.Null(resolved);
    }

    [Fact]
    public void ResolvePrinter_RoleRouting_DifferentiatesReceiptFromKitchenAndBar()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var receiptProfile = new PrinterProfile { Id = Guid.NewGuid(), ProfileName = "Front Receipt", Role = PrinterRole.Receipt, IsEnabled = true };
        var kitchenProfile = new PrinterProfile { Id = Guid.NewGuid(), ProfileName = "Kitchen Hot Prep", Role = PrinterRole.Kitchen, IsEnabled = true };
        var barProfile = new PrinterProfile { Id = Guid.NewGuid(), ProfileName = "Bar Dispense", Role = PrinterRole.Bar, IsEnabled = true };

        var config = new PrinterConfiguration
        {
            Profiles = [receiptProfile, kitchenProfile, barProfile],
            Assignments =
            [
                new PrinterAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, Role = PrinterRole.Receipt, PrinterProfileId = receiptProfile.Id },
                new PrinterAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, Role = PrinterRole.Kitchen, PrinterProfileId = kitchenProfile.Id },
                new PrinterAssignment { Id = Guid.NewGuid(), OrganizationId = orgId, Role = PrinterRole.Bar, PrinterProfileId = barProfile.Id }
            ]
        };

        var router = new PrinterRouter(config);

        // Act & Assert
        Assert.Equal(receiptProfile.Id, router.ResolvePrinter(orgId, null, null, PrinterRole.Receipt)?.Id);
        Assert.Equal(kitchenProfile.Id, router.ResolvePrinter(orgId, null, null, PrinterRole.Kitchen)?.Id);
        Assert.Equal(barProfile.Id, router.ResolvePrinter(orgId, null, null, PrinterRole.Bar)?.Id);
    }
}
