namespace Clovent.Identity.Users;

/// <summary>The lifecycle state of a <see cref="User"/> account.</summary>
public enum UserStatus
{
    /// <summary>Created but not yet activated.</summary>
    PendingActivation,

    /// <summary>Active and able to participate in the system.</summary>
    Active,

    /// <summary>Deliberately deactivated (e.g. offboarded).</summary>
    Inactive,

    /// <summary>Locked out, typically for a security reason; must be unlocked before it can be active again.</summary>
    Locked
}
