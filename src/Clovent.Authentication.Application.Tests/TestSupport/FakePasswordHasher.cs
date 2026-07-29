namespace Clovent.Authentication.Application.Tests.TestSupport;

/// <summary>Deterministic, non-cryptographic stand-in for <see cref="IPasswordHasher"/> - "hash" is just a recognizable prefix, sufficient to verify handler behavior without exercising real PBKDF2 work.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string hash) => hash == $"hashed:{password}";
}
