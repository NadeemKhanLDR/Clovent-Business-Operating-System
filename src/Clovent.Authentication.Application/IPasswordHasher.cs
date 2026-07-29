namespace Clovent.Authentication.Application;

/// <summary>
/// Computes and verifies password hashes. An Application-owned seam (the
/// same Dependency Inversion pattern as <see cref="IIdentityUserService"/>/
/// <see cref="IUnitOfWork"/>): actually deriving a hash is a
/// security/Infrastructure concern <c>AuthenticationDomain.md</c> Section 8
/// explicitly kept out of the Domain layer (<c>Credentials.PasswordHash</c>
/// only ever wraps an already-computed value, never computes one). No
/// implementation existed before Milestone 9 ("Authentication
/// Integration") - the first milestone that actually needs to check a
/// submitted password against a stored hash.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Computes a new, salted hash for <paramref name="password"/>.</summary>
    string Hash(string password);

    /// <summary>Verifies <paramref name="password"/> against a previously-computed <paramref name="hash"/>.</summary>
    bool Verify(string password, string hash);
}
