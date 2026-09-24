using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.Services;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Branches;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;
using Clovent.Identity.Organizations;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals;
using Clovent.MasterData.Terminals.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Services;

public class TerminalResolutionServiceTests : IDisposable
{
    private readonly Branch _branch;
    private readonly Warehouse _warehouse;

    private readonly FakeTerminalRepository _terminalRepo = new();
    private readonly FakeBranchRepository _branchRepo = new();
    private readonly FakeWarehouseRepository _warehouseRepo = new();
    private readonly FakeUserRepository _userRepo = new();
    private readonly FakeCompanyRepository _companyRepo = new();
    private readonly FakeOrganizationRepository _orgRepo = new();
    private readonly FakeCurrentSession _currentSession = new();

    public TerminalResolutionServiceTests()
    {
        Environment.SetEnvironmentVariable("CBOS_TERMINAL_ID", null);
        PosSettingsStore.SaveTerminalId(null);
        PosSettingsStore.SaveBranchId(null);

        _branch = Branch.Create(CompanyId.New(), BranchName.Create("Main Branch"));
        _branchRepo.Add(_branch);
        PosSettingsStore.SaveBranchId(_branch.Id.Value);

        _warehouse = Warehouse.Create(_branch.Id, WarehouseName.Create("Main Warehouse"), EntityCode.Create("WH-01"));
        _warehouseRepo.Add(_warehouse);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CBOS_TERMINAL_ID", null);
        PosSettingsStore.SaveTerminalId(null);
        PosSettingsStore.SaveBranchId(null);
    }

    private TerminalResolutionService CreateService()
    {
        return new TerminalResolutionService(
            _terminalRepo,
            _branchRepo,
            _warehouseRepo,
            _userRepo,
            _companyRepo,
            _orgRepo,
            _currentSession,
            NullLogger<TerminalResolutionService>.Instance);
    }

    [Fact]
    public async Task Resolve_WhenEnvVarIsGuid_ResolvesMatchingTerminal()
    {
        var term1 = Terminal.Create(_branch.Id, TerminalName.Create("Terminal One"), EntityCode.Create("T-001"));
        var term2 = Terminal.Create(_branch.Id, TerminalName.Create("Terminal Two"), EntityCode.Create("T-002"));
        _terminalRepo.Add(term1);
        _terminalRepo.Add(term2);

        Environment.SetEnvironmentVariable("CBOS_TERMINAL_ID", term2.Id.Value.ToString());

        var service = CreateService();
        var result = await service.ResolveCurrentTerminalAsync();

        Assert.True(result.IsConfigured);
        Assert.Equal(term2.Id.Value, result.TerminalId);
        Assert.Equal("Terminal Two", result.TerminalName);
        Assert.Equal("EnvironmentVariable", result.ResolutionSource);
        Assert.Equal(term2.Id.Value, PosSettingsStore.LoadTerminalId());
    }

    [Fact]
    public async Task Resolve_WhenEnvVarIsCode_ResolvesMatchingTerminal()
    {
        var term1 = Terminal.Create(_branch.Id, TerminalName.Create("Counter A"), EntityCode.Create("T-A"));
        var term2 = Terminal.Create(_branch.Id, TerminalName.Create("Counter B"), EntityCode.Create("T-B"));
        _terminalRepo.Add(term1);
        _terminalRepo.Add(term2);

        Environment.SetEnvironmentVariable("CBOS_TERMINAL_ID", "T-B");

        var service = CreateService();
        var result = await service.ResolveCurrentTerminalAsync();

        Assert.True(result.IsConfigured);
        Assert.Equal(term2.Id.Value, result.TerminalId);
        Assert.Equal("Counter B", result.TerminalName);
        Assert.Equal("EnvironmentVariable", result.ResolutionSource);
    }

    [Fact]
    public async Task Resolve_WhenEnvVarIsName_ResolvesMatchingTerminal()
    {
        var term1 = Terminal.Create(_branch.Id, TerminalName.Create("Front Counter"), EntityCode.Create("T-001"));
        _terminalRepo.Add(term1);

        Environment.SetEnvironmentVariable("CBOS_TERMINAL_ID", "Front Counter");

        var service = CreateService();
        var result = await service.ResolveCurrentTerminalAsync();

        Assert.True(result.IsConfigured);
        Assert.Equal(term1.Id.Value, result.TerminalId);
        Assert.Equal("Front Counter", result.TerminalName);
        Assert.Equal("EnvironmentVariable", result.ResolutionSource);
    }

    [Fact]
    public async Task Resolve_WhenPosSettingsHasValidTerminal_ResolvesSavedTerminal()
    {
        var term1 = Terminal.Create(_branch.Id, TerminalName.Create("Drive-Thru"), EntityCode.Create("T-DRV"));
        _terminalRepo.Add(term1);

        PosSettingsStore.SaveTerminalId(term1.Id.Value);

        var service = CreateService();
        var result = await service.ResolveCurrentTerminalAsync();

        Assert.True(result.IsConfigured);
        Assert.Equal(term1.Id.Value, result.TerminalId);
        Assert.Equal("Drive-Thru", result.TerminalName);
        Assert.Equal("SavedSettings", result.ResolutionSource);
    }

    [Fact]
    public async Task Resolve_WhenPosSettingsHasStaleTerminal_ClearsAndFallsBackToBranch()
    {
        var staleId = Guid.NewGuid();
        PosSettingsStore.SaveTerminalId(staleId);

        var validTerm = Terminal.Create(_branch.Id, TerminalName.Create("Counter"), EntityCode.Create("T-01"));
        _terminalRepo.Add(validTerm);

        var service = CreateService();
        var result = await service.ResolveCurrentTerminalAsync();

        Assert.True(result.IsConfigured);
        Assert.Equal(validTerm.Id.Value, result.TerminalId);
        Assert.Equal("SingleBranchTerminal", result.ResolutionSource);
        Assert.Equal(validTerm.Id.Value, PosSettingsStore.LoadTerminalId());
    }

    [Fact]
    public async Task Resolve_WhenMachineNameMatchesTerminalCode_ResolvesMatchingTerminal()
    {
        var machineName = Environment.MachineName;
        if (string.IsNullOrWhiteSpace(machineName)) return;

        var safeCode = machineName.Length > 20 ? machineName.Substring(0, 20) : machineName;
        safeCode = System.Text.RegularExpressions.Regex.Replace(safeCode, @"[^A-Za-z0-9-]", "-");

        var term = Terminal.Create(_branch.Id, TerminalName.Create("Machine Terminal"), EntityCode.Create(safeCode));
        _terminalRepo.Add(term);

        var service = CreateService();
        var result = await service.ResolveCurrentTerminalAsync();

        Assert.True(result.IsConfigured);
        Assert.Equal(term.Id.Value, result.TerminalId);
    }

    [Fact]
    public async Task Resolve_WhenSingleActiveTerminalOnBranch_AutoBindsAndSaves()
    {
        var term = Terminal.Create(_branch.Id, TerminalName.Create("Sole Counter"), EntityCode.Create("T-SOLE"));
        _terminalRepo.Add(term);

        var service = CreateService();
        var result = await service.ResolveCurrentTerminalAsync();

        Assert.True(result.IsConfigured);
        Assert.Equal(term.Id.Value, result.TerminalId);
        Assert.Equal("SingleBranchTerminal", result.ResolutionSource);
        Assert.Equal(term.Id.Value, PosSettingsStore.LoadTerminalId());
    }

    [Fact]
    public async Task Resolve_WhenMultipleActiveTerminalsOnBranchWithoutBinding_RequiresExplicitSelection()
    {
        var termZ = Terminal.Create(_branch.Id, TerminalName.Create("Zeta Counter"), EntityCode.Create("T-Z"));
        var termA = Terminal.Create(_branch.Id, TerminalName.Create("Alpha Counter"), EntityCode.Create("T-A"));
        var termM = Terminal.Create(_branch.Id, TerminalName.Create("Mu Counter"), EntityCode.Create("T-M"));

        // Added in unordered sequence
        _terminalRepo.Add(termZ);
        _terminalRepo.Add(termA);
        _terminalRepo.Add(termM);

        var service = CreateService();
        var result = await service.ResolveCurrentTerminalAsync();

        Assert.False(result.IsConfigured);
        Assert.Null(result.TerminalId);
        Assert.Contains("Multiple active terminals", result.ErrorMessage);
        Assert.Null(PosSettingsStore.LoadTerminalId());
    }

    #region Fakes

    private sealed class FakeTerminalRepository : ITerminalRepository
    {
        private readonly Dictionary<TerminalId, Terminal> _terminals = [];
        public void Add(Terminal terminal) => _terminals[terminal.Id] = terminal;
        public Task<Terminal?> GetByIdAsync(TerminalId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_terminals.GetValueOrDefault(id));
        public Task<IReadOnlyCollection<Terminal>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Terminal>>([.. _terminals.Values.Where(t => t.BranchId == branchId)]);
        public Task AddAsync(Terminal terminal, CancellationToken cancellationToken = default)
        {
            _terminals[terminal.Id] = terminal;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBranchRepository : IBranchRepository
    {
        private readonly Dictionary<BranchId, Branch> _branches = [];
        public void Add(Branch branch) => _branches[branch.Id] = branch;
        public Task<Branch?> GetByIdAsync(BranchId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_branches.GetValueOrDefault(id));
        public Task<IReadOnlyCollection<Branch>> GetByCompanyIdAsync(CompanyId companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Branch>>([.. _branches.Values.Where(b => b.CompanyId == companyId)]);
        public Task AddAsync(Branch branch, CancellationToken cancellationToken = default)
        {
            _branches[branch.Id] = branch;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWarehouseRepository : IWarehouseRepository
    {
        private readonly Dictionary<WarehouseId, Warehouse> _warehouses = [];
        public void Add(Warehouse warehouse) => _warehouses[warehouse.Id] = warehouse;
        public Task<Warehouse?> GetByIdAsync(WarehouseId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_warehouses.GetValueOrDefault(id));
        public Task<IReadOnlyCollection<Warehouse>> GetByBranchIdAsync(BranchId branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Warehouse>>([.. _warehouses.Values.Where(w => w.BranchId == branchId)]);
        public Task<IReadOnlyCollection<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Warehouse>>([.. _warehouses.Values]);
        public Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
        {
            _warehouses[warehouse.Id] = warehouse;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByUserNameAsync(UserName userName, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<IReadOnlyList<User>> SearchAsync(
            string? searchText = null,
            CompanyId? companyId = null,
            BranchId? branchId = null,
            RoleId? roleId = null,
            UserStatus? status = null,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>([]);
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCompanyRepository : ICompanyRepository
    {
        public Task<Company?> GetByIdAsync(CompanyId id, CancellationToken cancellationToken = default) => Task.FromResult<Company?>(null);
        public Task<IReadOnlyCollection<Company>> GetByCompanyIdAsync(CompanyId companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Company>>([]);
        public Task<IReadOnlyCollection<Company>> GetByOrganizationIdAsync(OrganizationId organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Company>>([]);
        public Task AddAsync(Company company, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeOrganizationRepository : IOrganizationRepository
    {
        public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default) => Task.FromResult<Organization?>(null);
        public Task<IReadOnlyCollection<Organization>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Organization>>([]);
        public Task AddAsync(Organization organization, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCurrentSession : ICurrentSession
    {
        public bool IsAuthenticated => false;
        public Guid? UserId => null;
        public Guid? SessionId => null;
        public string? DisplayName => null;
        public void SignIn(Guid userId, Guid sessionId, string displayName) { }
        public void SignOut() { }
        public event EventHandler? Changed { add { } remove { } }
    }

    #endregion
}
