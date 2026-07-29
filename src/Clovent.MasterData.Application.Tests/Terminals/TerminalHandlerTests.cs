using Clovent.Identity.Branches;
using Clovent.MasterData.Application.Terminals.Commands;
using Clovent.MasterData.Application.Terminals.Queries;
using Clovent.MasterData.Application.Tests.TestSupport;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Terminals.ValueObjects;
using Xunit;

namespace Clovent.MasterData.Application.Tests.Terminals;

public class TerminalHandlerTests
{
    [Fact]
    public async Task CreateTerminalCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeTerminalRepository();
        var handler = new CreateTerminalCommandHandler(repository);

        var dto = await handler.Handle(new CreateTerminalCommand(BranchId.New().Value, "Front Counter", "T-001"), CancellationToken.None);

        Assert.Equal("Front Counter", dto.Name);
        Assert.Equal("T-001", dto.Code);
        Assert.NotNull(await repository.GetByIdAsync(new TerminalId(dto.TerminalId)));
    }

    [Fact]
    public async Task RenameTerminalCommandHandler_UnknownTerminal_Throws()
    {
        var handler = new RenameTerminalCommandHandler(new FakeTerminalRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameTerminalCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateTerminalCommandHandlers_RoundTrip()
    {
        var repository = new FakeTerminalRepository();
        var terminal = Terminal.Create(BranchId.New(), TerminalName.Create("Front Counter"), EntityCode.Create("T-001"));
        terminal.Deactivate();
        repository.Add(terminal);

        var activated = await new ActivateTerminalCommandHandler(repository)
            .Handle(new ActivateTerminalCommand(terminal.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateTerminalCommandHandler(repository)
            .Handle(new DeactivateTerminalCommand(terminal.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetTerminalByIdQueryHandler_UnknownTerminal_Throws()
    {
        var handler = new GetTerminalByIdQueryHandler(new FakeTerminalRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetTerminalByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListTerminalsByBranchQueryHandler_FiltersToOwningBranch()
    {
        var repository = new FakeTerminalRepository();
        var branchId = BranchId.New();
        repository.Add(Terminal.Create(branchId, TerminalName.Create("Terminal A"), EntityCode.Create("T-001")));
        repository.Add(Terminal.Create(BranchId.New(), TerminalName.Create("Terminal B"), EntityCode.Create("T-002")));
        var handler = new ListTerminalsByBranchQueryHandler(repository);

        var result = await handler.Handle(new ListTerminalsByBranchQuery(branchId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
