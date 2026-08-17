namespace Clovent.Desktop.Authorization;

/// <summary>
/// Challenges for a manager's own credentials before a privileged action the
/// signed-in operator is not entitled to perform on their own.
/// </summary>
/// <remarks>
/// <para>
/// This is an <em>authorization step inside an existing session</em>, not a
/// second sign-in: it never touches <see cref="Sessions.ICurrentSession"/>,
/// issues no session or refresh token, and leaves the cashier signed in as
/// themselves throughout. What it does do is exactly what
/// <see cref="Login.ILoginService"/> does to establish that a credential is
/// genuine - resolve the user through Identity, verify the submitted secret
/// against the stored hash with the same <c>IPasswordHasher</c>/<c>IPinHasher</c>,
/// and record the attempt through Authentication's own
/// <c>RecordLoginAttemptCommand</c>/<c>RecordCredentialCheckCommand</c> so
/// failures count towards the same lockout policy every other credential
/// check does. There is deliberately no separate credential store, no
/// separate hashing, and no "manager PIN" kept anywhere in the UI layer.
/// </para>
/// <para>
/// Verifying the credential is necessary but not sufficient: the resolved
/// user must additionally hold the permission the action requires, so an
/// ordinary cashier cannot authorize an override merely by knowing another
/// cashier's password.
/// </para>
/// </remarks>
public interface IManagerAuthorizationService
{
    /// <summary>
    /// Verifies the submitted credentials and confirms the resolved user
    /// holds <paramref name="featureCode"/>.
    /// </summary>
    /// <param name="userName">The manager's username or email address.</param>
    /// <param name="password">The manager's password.</param>
    /// <param name="featureCode">
    /// The feature the manager must be entitled to (e.g.
    /// <c>pos.exceedcreditlimit</c>), checked through the same
    /// <c>IFeatureAuthorizationPolicy</c> every other permission check uses.
    /// </param>
    /// <param name="cancellationToken">Cancels the check.</param>
    Task<ManagerAuthorizationResult> AuthorizeAsync(
        string userName,
        string password,
        string featureCode,
        CancellationToken cancellationToken = default);
}

/// <summary>The outcome of a manager authorization challenge.</summary>
/// <param name="Succeeded">Whether the action may proceed.</param>
/// <param name="ManagerUserId">The authorizing manager, when <paramref name="Succeeded"/> is <see langword="true"/>.</param>
/// <param name="ManagerDisplayName">The authorizing manager's display name, for the audit entry.</param>
/// <param name="ErrorMessage">
/// A message safe to show the operator. Deliberately does not distinguish
/// "no such user" from "wrong password" - only a refused permission is
/// reported specifically, since that is not a credential-guessing signal.
/// </param>
public sealed record ManagerAuthorizationResult(
    bool Succeeded,
    Guid? ManagerUserId,
    string? ManagerDisplayName,
    string? ErrorMessage)
{
    /// <summary>The manager authenticated and holds the required permission.</summary>
    public static ManagerAuthorizationResult Approved(Guid managerUserId, string managerDisplayName) =>
        new(true, managerUserId, managerDisplayName, null);

    /// <summary>The challenge failed; the caller must change nothing.</summary>
    public static ManagerAuthorizationResult Denied(string errorMessage) =>
        new(false, null, null, errorMessage);
}
