using Clovent.Authentication.Application;

namespace Clovent.Authentication.Infrastructure.Security;

/// <summary><see cref="IPasswordHasher"/> implementation over <see cref="Pbkdf2Hash"/>.</summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    /// <inheritdoc/>
    public string Hash(string password) => Pbkdf2Hash.Hash(password);

    /// <inheritdoc/>
    public bool Verify(string password, string hash) => Pbkdf2Hash.Verify(password, hash);
}
