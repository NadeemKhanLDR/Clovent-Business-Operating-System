using Clovent.Identity.Branches;
using Clovent.MasterData;
using Clovent.MasterData.Shared;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Terminals.Events;
using Clovent.MasterData.Terminals.ValueObjects;
using Xunit;

namespace Clovent.MasterData.Tests.Terminals;

public class TerminalTests
{
    [Fact]
    public void Create_SetsFields_ActiveByDefault_RaisesTerminalCreated()
    {
        var branchId = BranchId.New();

        var terminal = Terminal.Create(branchId, TerminalName.Create("Front Counter"), EntityCode.Create("T-001"));

        Assert.Equal(branchId, terminal.BranchId);
        Assert.Equal("T-001", terminal.Code.Value);
        Assert.Equal(MasterDataStatus.Active, terminal.Status);
        Assert.IsType<TerminalCreated>(Assert.Single(terminal.DomainEvents));
    }

    [Fact]
    public void Activate_AlreadyActive_Throws()
    {
        var terminal = Terminal.Create(BranchId.New(), TerminalName.Create("Front Counter"), EntityCode.Create("T-001"));

        Assert.Throws<MasterDataDomainException>(() => terminal.Activate());
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var terminal = Terminal.Create(BranchId.New(), TerminalName.Create("Front Counter"), EntityCode.Create("T-001"));
        terminal.ClearDomainEvents();

        terminal.Deactivate();
        Assert.Equal(MasterDataStatus.Inactive, terminal.Status);
        Assert.IsType<TerminalDeactivated>(Assert.Single(terminal.DomainEvents));

        terminal.Activate();
        Assert.Equal(MasterDataStatus.Active, terminal.Status);
    }
}
