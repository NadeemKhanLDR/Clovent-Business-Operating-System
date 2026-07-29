using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.MasterData.Shared;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Terminals.Events;
using Clovent.MasterData.Terminals.ValueObjects;

namespace Clovent.MasterData.Terminals;

/// <summary>
/// A physical or virtual point of sale/access terminal within a
/// <see cref="Branch"/> (e.g. a POS register, a self-service kiosk). Models
/// only identity/lifecycle/naming - any terminal-specific behavior (till
/// sessions, hardware pairing) is a future module's concern (Restaurant
/// POS), explicitly out of scope here. References its owning branch by id
/// only - see <see cref="Departments.Department"/>'s identical doc comment.
/// </summary>
public sealed class Terminal : AggregateRoot<TerminalId>
{
    /// <summary>The branch this terminal belongs to, fixed at creation.</summary>
    public BranchId BranchId { get; }

    /// <summary>The terminal's display name.</summary>
    public TerminalName Name { get; private set; }

    /// <summary>The terminal's short code (e.g. "T-001").</summary>
    public EntityCode Code { get; }

    /// <summary>The terminal's current lifecycle state.</summary>
    public MasterDataStatus Status { get; private set; }

    /// <summary>UTC instant this terminal was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Terminal(TerminalId id, BranchId branchId, TerminalName name, EntityCode code, MasterDataStatus status, DateTimeOffset createdAtUtc)
    {
        Id = id;
        BranchId = branchId;
        Name = name;
        Code = code;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>Creates a new, active terminal under the given branch.</summary>
    public static Terminal Create(BranchId branchId, TerminalName name, EntityCode code)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(code);

        var now = DateTimeOffset.UtcNow;
        var terminal = new Terminal(TerminalId.New(), branchId, name, code, MasterDataStatus.Active, now);
        terminal.AddDomainEvent(new TerminalCreated(terminal.Id, terminal.BranchId, terminal.Name, terminal.Code, now));
        return terminal;
    }

    /// <summary>Renames the terminal. A no-op (no event raised) if unchanged.</summary>
    public void Rename(TerminalName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new TerminalRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Activates the terminal.</summary>
    /// <exception cref="MasterDataDomainException">The terminal is already active.</exception>
    public void Activate()
    {
        if (Status == MasterDataStatus.Active)
            throw MasterDataDomainException.TerminalAlreadyActive(Id);

        Status = MasterDataStatus.Active;
        AddDomainEvent(new TerminalActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates the terminal.</summary>
    /// <exception cref="MasterDataDomainException">The terminal is not active.</exception>
    public void Deactivate()
    {
        if (Status != MasterDataStatus.Active)
            throw MasterDataDomainException.TerminalNotActive(Id);

        Status = MasterDataStatus.Inactive;
        AddDomainEvent(new TerminalDeactivated(Id, DateTimeOffset.UtcNow));
    }
}
