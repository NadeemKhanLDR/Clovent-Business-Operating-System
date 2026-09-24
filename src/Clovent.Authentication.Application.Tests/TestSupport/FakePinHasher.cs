using Clovent.Authentication.Application;

namespace Clovent.Authentication.Application.Tests.TestSupport;

/// <summary>Deterministic, non-cryptographic stand-in for <see cref="IPinHasher"/> - "hash" is just a recognizable prefix, sufficient to verify handler behavior without exercising real PBKDF2 work.</summary>
internal sealed class FakePinHasher : IPinHasher
{
    public string Hash(string pin) => $"pin:{pin}";

    public bool Verify(string pin, string hash) => hash == $"pin:{pin}";
}
