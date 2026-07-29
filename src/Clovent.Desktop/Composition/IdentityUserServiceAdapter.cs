using Clovent.Authentication.Application;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Users;

namespace Clovent.Desktop.Composition;

/// <summary>
/// Implements Authentication's <see cref="IIdentityUserService"/> seam by
/// adapting Identity's <see cref="IUserRepository"/> - exactly the "future
/// Infrastructure/Application-composition milestone" both
/// <c>AuthenticationDomain.md</c> Section 9 and <c>IdentityDomain.md</c>
/// Section 8 (item 2) anticipated, calling <see cref="User.Lock"/> from
/// outside the Authentication project, where that coupling across the two
/// bounded contexts is legitimate. Lives in <c>Clovent.Desktop</c> - the
/// composition root that is allowed to see both bounded contexts - rather
/// than in either bounded context's own Infrastructure project, so neither
/// gains a reference to the other.
/// </summary>
/// <remarks>
/// Takes <see cref="IdentityDbContext"/> directly (rather than an
/// <c>IIdentityUnitOfWork</c> abstraction) to persist <see cref="LockUserAsync"/>'s
/// mutation: Identity has no Application layer yet, so no other caller
/// needs such an abstraction, and this adapter is already a cross-boundary
/// composition-root concern, not something either bounded context's own
/// code depends on. <c>RecordLoginAttemptCommand</c>'s own
/// <c>AuthenticationDbContext</c> is committed by Authentication's
/// <c>UnitOfWorkBehavior</c> pipeline - a completely separate DbContext,
/// which is exactly why this adapter must commit Identity's side itself.
/// </remarks>
public sealed class IdentityUserServiceAdapter(IUserRepository userRepository, IdentityDbContext identityDbContext) : IIdentityUserService
{
    /// <inheritdoc/>
    public async Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(new UserId(userId), cancellationToken);
        return user?.Status == UserStatus.Active;
    }

    /// <inheritdoc/>
    public async Task LockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(new UserId(userId), cancellationToken);
        if (user is null)
        {
            return;
        }

        user.Lock();
        await identityDbContext.SaveChangesAsync(cancellationToken);
    }
}
