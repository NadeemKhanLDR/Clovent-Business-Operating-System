namespace Clovent.Authentication.Application;

/// <summary>Computes and verifies PIN hashes. See <see cref="IPasswordHasher"/> for the identical reasoning - kept as a separate interface for the same type-safety reason <c>Credentials.PinHash</c> is kept distinct from <c>Credentials.PasswordHash</c>.</summary>
public interface IPinHasher
{
    /// <summary>Computes a new, salted hash for <paramref name="pin"/>.</summary>
    string Hash(string pin);

    /// <summary>Verifies <paramref name="pin"/> against a previously-computed <paramref name="hash"/>.</summary>
    bool Verify(string pin, string hash);
}
