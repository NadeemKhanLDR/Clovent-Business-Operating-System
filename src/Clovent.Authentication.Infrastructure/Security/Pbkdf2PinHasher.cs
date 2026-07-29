using Clovent.Authentication.Application;

namespace Clovent.Authentication.Infrastructure.Security;

/// <summary><see cref="IPinHasher"/> implementation over <see cref="Pbkdf2Hash"/>.</summary>
public sealed class Pbkdf2PinHasher : IPinHasher
{
    /// <inheritdoc/>
    public string Hash(string pin) => Pbkdf2Hash.Hash(pin);

    /// <inheritdoc/>
    public bool Verify(string pin, string hash) => Pbkdf2Hash.Verify(pin, hash);
}
