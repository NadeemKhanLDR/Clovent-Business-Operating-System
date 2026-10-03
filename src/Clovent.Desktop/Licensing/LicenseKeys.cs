namespace Clovent.Desktop.Licensing;

/// <summary>
/// Embedded public keys used for offline verification of digitally signed licenses.
/// Supports cryptographic key rotation and versioning (Requirement 19).
/// Note: The private signing key is held strictly by the software vendor and is never included in client binaries.
/// </summary>
public static class LicenseKeys
{
    public const string ActiveKeyId = "clovent-2026-v2";
    public const string RevokedKeyIdV1 = "clovent-2026-v1";

    public const string V2PublicKeyXml = "<RSAKeyValue><Modulus>vv1zfdZvAKdbqArGlUeZ/1PkIXTtinSl1gsM8TyHq3tM7KmSR3YjmrsnS3HFpO31HkfiRyXq46Gc9XGeu0Duc+A8gCCeJKYRI8/1er5e2fjk6+66+xEElKFCKH060PicSkLGmpOX/ocUbq8eTDtCLSTx+2cjJwMMAWyNobPLthRtFdrVnKEKL5EXoZkcAXbZjxZZugH+8cQsy1BfRqTuHxR5mU7Wf0D5HVufQKS2+9tOkxHXed4neSFJdE5hMDO7Ru0rDBDeQLSFrE2j+UuC/Q/Bm8anVzg+m0svBOCIPK4sxh0izLTFca06NLItXLLpMdIplgOpAPa4UJJ123QfwQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    public const string V1PublicKeyXml = "<RSAKeyValue><Modulus>lB6M5DppGtCvGMr8nl/3EJ1oJ+5l2XpksqDm0F2nKRYeOLyYJef6Xh8ukTihvF4W23pRkpcjXyWVfMeKJa69z0aO5A92hyi5TcbMnwT28kCho8eGabfCSW24le02WHqbDuUzqXq6qMFK5gfCsYHSRndborpI+guVmubCeS9ckvuSzco3EV28ePjXAB8d9Bj5Fg9LHpxDNCDlGPlPn9qTtVNaMcSMzfM4oWaoM8bao3rTGJYGSzIIsOU5vTYZGeBh60pKkhlsUs4vK7q52FNwWpoDRRi3A/qDyvkkU12UcUSykTkpqGakGZSmw5x8C/JWrG5Hfu9GYvOT+8dHOZlSsQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    /// <summary>
    /// Default public key (pointing to current active key clovent-2026-v2).
    /// </summary>
    public const string PublicKeyXml = V2PublicKeyXml;

    private static readonly Dictionary<string, string> PublicKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        [ActiveKeyId] = V2PublicKeyXml,
        [RevokedKeyIdV1] = V1PublicKeyXml
    };

    private static readonly HashSet<string> RevokedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        RevokedKeyIdV1
    };

    /// <summary>
    /// Retrieves the embedded RSA public key XML for the specified KeyId.
    /// </summary>
    public static string? GetPublicKey(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
        {
            return null;
        }

        return PublicKeys.TryGetValue(keyId, out var xml) ? xml : null;
    }

    /// <summary>
    /// Checks whether the specified KeyId has been revoked or marked compromised.
    /// </summary>
    public static bool IsKeyRevoked(string keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
        {
            return false;
        }

        return RevokedKeys.Contains(keyId);
    }
}
