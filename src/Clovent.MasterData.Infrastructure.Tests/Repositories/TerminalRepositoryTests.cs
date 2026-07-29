using Clovent.Identity.Branches;
using Clovent.MasterData.Infrastructure.Repositories;
using Clovent.MasterData.Infrastructure.Tests.TestSupport;
using Clovent.MasterData.Shared;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Terminals.ValueObjects;
using Xunit;

namespace Clovent.MasterData.Infrastructure.Tests.Repositories;

public class TerminalRepositoryTests : SqliteTestBase
{
    private static Terminal CreateTerminal(BranchId branchId, string name = "Front Counter", string code = "T-001") =>
        Terminal.Create(branchId, TerminalName.Create(name), EntityCode.Create(code));

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var branchId = BranchId.New();
        var terminal = CreateTerminal(branchId);

        await using (var writeContext = CreateContext())
        {
            var repository = new TerminalRepository(writeContext);
            await repository.AddAsync(terminal);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new TerminalRepository(readContext).GetByIdAsync(terminal.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(branchId, reloaded!.BranchId);
        Assert.Equal(terminal.Name, reloaded.Name);
        Assert.Equal(terminal.Code, reloaded.Code);
        Assert.Equal(MasterDataStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetByBranchIdAsync_FiltersToOwningBranch()
    {
        var branchId = BranchId.New();
        var otherBranchId = BranchId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new TerminalRepository(writeContext);
            await repository.AddAsync(CreateTerminal(branchId, "Terminal A", "T-001"));
            await repository.AddAsync(CreateTerminal(branchId, "Terminal B", "T-002"));
            await repository.AddAsync(CreateTerminal(otherBranchId, "Terminal C", "T-003"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new TerminalRepository(readContext).GetByBranchIdAsync(branchId);

        Assert.Equal(2, found.Count);
        Assert.All(found, t => Assert.Equal(branchId, t.BranchId));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new TerminalRepository(context).GetByIdAsync(TerminalId.New());

        Assert.Null(result);
    }
}
