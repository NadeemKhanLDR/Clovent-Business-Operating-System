namespace Clovent.Authentication.LoginAttempts;

/// <summary>The result of a single login attempt.</summary>
public enum LoginOutcome
{
    /// <summary>Credentials were valid and the attempt succeeded.</summary>
    Succeeded,

    /// <summary>No user matches the attempted identifier.</summary>
    UserNotFound,

    /// <summary>The credentials supplied did not match the user's stored credentials.</summary>
    InvalidCredentials,

    /// <summary>The user matched but is not active (e.g. pending activation or deactivated).</summary>
    UserInactive,

    /// <summary>The user matched but is locked out.</summary>
    UserLocked
}
