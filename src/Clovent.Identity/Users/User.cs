using Clovent.Domain;
using Clovent.Identity.Roles;
using Clovent.Identity.Users.Events;
using Clovent.Identity.Users.ValueObjects;

namespace Clovent.Identity.Users;

/// <summary>
/// A person or service account that can act within CBOS. Owns its lifecycle
/// (<see cref="UserStatus"/>) and its set of assigned <see cref="Role"/>s;
/// says nothing about how it authenticates - that is a separate bounded
/// context layered on top of this one.
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
    private readonly HashSet<RoleId> _roleIds;

    /// <summary>The user's contact email address.</summary>
    public Email Email { get; private set; }

    /// <summary>The user's login handle.</summary>
    public UserName UserName { get; private set; }

    /// <summary>The name shown for this user throughout the product.</summary>
    public DisplayName DisplayName { get; private set; }

    /// <summary>The user's current lifecycle state.</summary>
    public UserStatus Status { get; private set; }

    /// <summary>The roles currently assigned to this user.</summary>
    public IReadOnlyCollection<RoleId> RoleIds => _roleIds;

    /// <summary>UTC instant this user was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Takes every persisted field explicitly (rather than hardcoding
    /// <see cref="UserStatus.PendingActivation"/>, <c>DateTimeOffset.UtcNow</c>,
    /// and an empty role set) so this is the single, unambiguous constructor
    /// an EF Core Infrastructure implementation can bind to when
    /// materializing an existing user from storage - a constructor that
    /// only accepted "just created" parameters would silently reset those
    /// fields on every load. <paramref name="roleIds"/> in particular is how
    /// Milestone 10 ("Authorization") maps <see cref="RoleIds"/> without an
    /// EF owned-collection navigation: the property has no public setter and
    /// no field of a matching type, so materialization goes through this
    /// constructor exactly like every other field, rather than EF setting
    /// the backing field via reflection.
    /// </summary>
    private User(UserId id, Email email, UserName userName, DisplayName displayName, UserStatus status, DateTimeOffset createdAtUtc, IReadOnlyCollection<RoleId> roleIds)
    {
        Id = id;
        Email = email;
        UserName = userName;
        DisplayName = displayName;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        _roleIds = [.. roleIds];
    }

    /// <summary>Creates a new user in <see cref="UserStatus.PendingActivation"/>, with no roles assigned.</summary>
    public static User Create(Email email, UserName userName, DisplayName displayName)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(userName);
        ArgumentNullException.ThrowIfNull(displayName);

        var now = DateTimeOffset.UtcNow;
        var user = new User(UserId.New(), email, userName, displayName, UserStatus.PendingActivation, now, []);
        user.AddDomainEvent(new UserCreated(user.Id, user.Email, user.UserName, now));
        return user;
    }

    /// <summary>Activates the user, making it able to participate in the system.</summary>
    /// <exception cref="IdentityDomainException">The user is locked or already active.</exception>
    public void Activate()
    {
        if (Status == UserStatus.Locked)
            throw IdentityDomainException.UserMustBeUnlockedBeforeActivation(Id);
        if (Status == UserStatus.Active)
            throw IdentityDomainException.UserAlreadyActive(Id);

        Status = UserStatus.Active;
        AddDomainEvent(new UserActivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Deactivates an active user.</summary>
    /// <exception cref="IdentityDomainException">The user is not currently active.</exception>
    public void Deactivate()
    {
        if (Status != UserStatus.Active)
            throw IdentityDomainException.UserNotActive(Id);

        Status = UserStatus.Inactive;
        AddDomainEvent(new UserDeactivated(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Locks an active user out.</summary>
    /// <exception cref="IdentityDomainException">The user is not currently active.</exception>
    public void Lock()
    {
        if (Status != UserStatus.Active)
            throw IdentityDomainException.UserNotActive(Id);

        Status = UserStatus.Locked;
        AddDomainEvent(new UserLocked(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Unlocks a locked user, returning it to <see cref="UserStatus.Active"/>.</summary>
    /// <exception cref="IdentityDomainException">The user is not currently locked.</exception>
    public void Unlock()
    {
        if (Status != UserStatus.Locked)
            throw IdentityDomainException.UserNotLocked(Id);

        Status = UserStatus.Active;
        AddDomainEvent(new UserUnlocked(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Assigns a role to this user.</summary>
    /// <exception cref="IdentityDomainException">The role is already assigned.</exception>
    public void AssignRole(RoleId roleId)
    {
        if (!_roleIds.Add(roleId))
            throw IdentityDomainException.RoleAlreadyAssignedToUser(Id, roleId);

        AddDomainEvent(new UserRoleAssigned(Id, roleId, DateTimeOffset.UtcNow));
    }

    /// <summary>Removes a role from this user.</summary>
    /// <exception cref="IdentityDomainException">The role is not currently assigned.</exception>
    public void RemoveRole(RoleId roleId)
    {
        if (!_roleIds.Remove(roleId))
            throw IdentityDomainException.RoleNotAssignedToUser(Id, roleId);

        AddDomainEvent(new UserRoleRemoved(Id, roleId, DateTimeOffset.UtcNow));
    }

    /// <summary>Changes the name shown for this user. A no-op (no event raised) if unchanged.</summary>
    public void ChangeDisplayName(DisplayName displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        if (DisplayName == displayName) return;

        DisplayName = displayName;
        AddDomainEvent(new UserDisplayNameChanged(Id, displayName, DateTimeOffset.UtcNow));
    }
}
