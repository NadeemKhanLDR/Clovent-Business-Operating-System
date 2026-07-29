namespace Clovent.Authentication.RefreshSessions;

/// <summary>The lifecycle state of a <see cref="RefreshSession"/>.</summary>
public enum RefreshSessionStatus
{
    /// <summary>Live and eligible to be rotated, revoked, or expired.</summary>
    Active,

    /// <summary>Superseded by a newer refresh session via <see cref="RefreshSession.Rotate"/>.</summary>
    Rotated,

    /// <summary>Ended by administrative or security action.</summary>
    Revoked,

    /// <summary>Ended because its expiry instant was reached.</summary>
    Expired
}
