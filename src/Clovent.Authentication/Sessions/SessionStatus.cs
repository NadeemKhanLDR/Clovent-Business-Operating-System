namespace Clovent.Authentication.Sessions;

/// <summary>The lifecycle state of a <see cref="Session"/>.</summary>
public enum SessionStatus
{
    /// <summary>Live and eligible to be used or extended.</summary>
    Active,

    /// <summary>Ended because its idle timeout elapsed.</summary>
    Expired,

    /// <summary>Ended by administrative or security action.</summary>
    Revoked,

    /// <summary>Ended by the user's own explicit sign-out.</summary>
    LoggedOut
}
