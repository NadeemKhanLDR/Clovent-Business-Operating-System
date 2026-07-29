namespace Clovent.Authentication.Application;

/// <summary>
/// The capability Authentication needs from Identity's <c>User</c> lifecycle,
/// expressed in Authentication's own vocabulary and owned by Authentication
/// (the Dependency Inversion Principle applied at the module boundary) -
/// Authentication depends on this abstraction, never on
/// <c>Clovent.Identity.Users.IUserRepository</c> or <c>User</c> directly.
/// No implementation exists yet: like every repository interface in this
/// solution, this is the seam a future milestone implements against,
/// presumably by adapting <c>Clovent.Identity.Users.IUserRepository</c> and
/// calling <c>User.Lock()</c>/<c>Unlock()</c> from outside this project.
/// </summary>
public interface IIdentityUserService
{
    /// <summary>Whether the user is currently active and eligible for a security action such as a lockout.</summary>
    Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Locks the user's account.</summary>
    Task LockUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Unlocks the user's account - the symmetric counterpart to <see cref="LockUserAsync"/>.</summary>
    Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
