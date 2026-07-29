using System.Security.Cryptography;

namespace Clovent.Authentication.Infrastructure.Security;

/// <summary>
/// The PBKDF2 hashing routine shared by <see cref="Pbkdf2PasswordHasher"/>
/// and <see cref="Pbkdf2PinHasher"/> - kept in exactly one place rather than
/// duplicated between them, since this is security-sensitive code where a
/// copy-paste drift between two "identical" implementations is a real risk.
/// Uses only <see cref="Rfc2898DeriveBytes"/> (BCL, no third-party package) -
/// deliberately not <c>Microsoft.Extensions.Identity.Core</c>'s hasher, per
/// <c>09.02 Identity Package Inventory.md</c>'s caution against coupling the
/// product to that package without an explicit architect decision.
/// </summary>
internal static class Pbkdf2Hash
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 210_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    /// <summary>Computes a self-contained hash string (iterations, salt, and derived key, all Base64-encoded) for <paramref name="value"/>.</summary>
    public static string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(value, salt, Iterations, Algorithm, HashSize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    /// <summary>Verifies <paramref name="value"/> against a hash produced by <see cref="Hash"/>, in constant time.</summary>
    public static bool Verify(string value, string hash)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        var parts = hash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedKey;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedKey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(value, salt, iterations, Algorithm, expectedKey.Length);
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
