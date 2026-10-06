using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clovent.LicenseIssuer;

public static class Program
{
    public const string KeyId = "clovent-2026-v2";

    public static int Main(string[] args)
    {
        if (args.Length == 0 || IsHelpRequested(args[0]))
        {
            PrintUsage();
            return 0;
        }

        var rawCommand = args[0];
        var command = rawCommand.TrimStart('-').ToLowerInvariant();
        var options = ParseOptions(args);

        try
        {
            return command switch
            {
                "generate-keys" or "generate-key" => HandleGenerateKeys(options),
                "issue" or "issue-license" => HandleIssue(options),
                "verify" or "verify-license" => HandleVerify(options),
                _ => PrintUnknownCommand(rawCommand)
            };
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"\n[ERROR] {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"Details: {ex.InnerException.Message}");
            }
            Console.ResetColor();
            return 1;
        }
    }

    private static bool IsHelpRequested(string arg) =>
        arg is "--help" or "-h" or "/?" or "help" or "-help";

    private static int PrintUnknownCommand(string command)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Unknown command: '{command}'");
        Console.ResetColor();
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"
================================================================================
Clovent Business Operating System - Standalone Vendor License Issuer Tool
================================================================================
NOTE: This tool is strictly for Clovent vendor operations and internal key management.
It must never be shipped to clients or included in client distributions.

USAGE:
  dotnet run --project tools\LicenseIssuer -- <command> [options]

COMMANDS:
  generate-keys [--out <path>]
      Generates a new RSA-2048 vendor key pair (KeyId: clovent-2026-v2).
      Private and public keys are saved in both PEM and XML formats.
      Default location: %USERPROFILE%\.clovent\keys\
      CRITICAL: Output directory must NOT reside inside the Git repository!

  issue [options]
      Issues and cryptographically signs a Clovent software license (.lic file).

      Required Options:
        --customer <name>       Customer / primary contact name
        --company <name>        Company / organization name
        --type <type>           Trial | Subscription | Perpetual
        --days <n>              Validity duration in days (for Perpetual, defaults to 36500)
        --modules <m1,m2>       Comma-separated modules (e.g. POS,BackOffice,Inventory,Catalog,Reporting,Restaurant)
        --terminals <n>         Maximum allowed terminals (0 = unlimited)
        --branches <n>          Maximum allowed branches (0 = unlimited)

      Optional Options:
        --machine-id <id>       Hardware machine fingerprint for node-locking
        --maintenance-days <n>  Maintenance window in days
        --out <file.lic>        Output license file path (default: clovent.lic)
        --key-file <path>       Explicit path to RSA private key (PEM or XML)

      Private Key Loading Precedence:
        1. Explicit --key-file <path>
        2. Environment variable CLOVENT_LICENSE_SIGNING_KEY (file path or inline PEM/XML)
        3. Standard external path: %USERPROFILE%\.clovent\keys\clovent_vendor_private_key.pem

  verify --license <file.lic> [--public-key <file|xml>]
      Cryptographically verifies a signed license file.

EXAMPLES:
  # Generate new production RSA-2048 key pair
  dotnet run --project tools\LicenseIssuer -- generate-keys

  # Issue standard commercial subscription license
  dotnet run --project tools\LicenseIssuer -- issue \
      --customer ""John Doe"" \
      --company ""Acme Retailers"" \
      --type Subscription \
      --days 365 \
      --modules POS,BackOffice,Inventory,Reporting \
      --terminals 5 \
      --branches 2 \
      --out ""licenses\acme.lic""

  # Issue perpetual license bound to hardware
  dotnet run --project tools\LicenseIssuer -- issue \
      --customer ""Enterprise Admin"" \
      --company ""Global Logistics Ltd"" \
      --type Perpetual \
      --days 36500 \
      --modules POS,BackOffice,Inventory,Catalog,Reporting,Restaurant \
      --terminals 0 \
      --branches 10 \
      --machine-id ""E320-A54D-9A69-A8F8"" \
      --out ""licenses\enterprise.lic""
================================================================================
");
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                var key = arg.Substring(2);
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    options[key] = args[i + 1];
                    i++;
                }
                else
                {
                    options[key] = "true";
                }
            }
            else if (arg.StartsWith("-", StringComparison.Ordinal))
            {
                var key = arg.Substring(1);
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                {
                    options[key] = args[i + 1];
                    i++;
                }
                else
                {
                    options[key] = "true";
                }
            }
        }
        return options;
    }

    #region Git Repository Protection
    public static string? FindGitRepositoryRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            var gitDir = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(gitDir) || File.Exists(gitDir))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    public static void EnsureNotInsideGitRepo(string path, string contextDescription)
    {
        var repoRoot = FindGitRepositoryRoot(AppContext.BaseDirectory)
                       ?? FindGitRepositoryRoot(Directory.GetCurrentDirectory());

        if (string.IsNullOrEmpty(repoRoot))
        {
            return;
        }

        var normalizedRepo = Path.GetFullPath(repoRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (normalizedPath.Equals(normalizedRepo, StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith(normalizedRepo + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"[SECURITY VIOLATION] {contextDescription} ('{normalizedPath}') is located inside the Git repository ('{normalizedRepo}').\n" +
                "Vendor private signing keys must NEVER reside within or be generated inside the repository source tree.\n" +
                "Store keys securely in an external location such as '%USERPROFILE%\\.clovent\\keys\\'.");
        }
    }
    #endregion

    #region Key Management
    private static RSA ResolveAndLoadPrivateKey(string? explicitKeyFile)
    {
        // 1) Explicit --key-file <path>
        if (!string.IsNullOrWhiteSpace(explicitKeyFile))
        {
            var fullPath = Path.GetFullPath(explicitKeyFile);
            EnsureNotInsideGitRepo(fullPath, "Explicit private key file");
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Explicit private key file not found: {fullPath}");
            }
            Console.WriteLine($"[Key Source] 1) Loaded from explicit argument: {fullPath}");
            return LoadRsaFromContent(File.ReadAllText(fullPath));
        }

        // 2) Environment variable CLOVENT_LICENSE_SIGNING_KEY (path or pem/xml text)
        var envKey = Environment.GetEnvironmentVariable("CLOVENT_LICENSE_SIGNING_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            if (envKey.Contains("-----BEGIN") || envKey.Contains("<RSAKeyValue>"))
            {
                Console.WriteLine("[Key Source] 2) Loaded directly from environment variable CLOVENT_LICENSE_SIGNING_KEY (inline text)");
                return LoadRsaFromContent(envKey);
            }

            var fullPath = Path.GetFullPath(envKey);
            EnsureNotInsideGitRepo(fullPath, "Environment variable private key file");
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Private key file specified in CLOVENT_LICENSE_SIGNING_KEY not found: {fullPath}");
            }
            Console.WriteLine($"[Key Source] 2) Loaded from environment variable path: {fullPath}");
            return LoadRsaFromContent(File.ReadAllText(fullPath));
        }

        // 3) Standard external path: %USERPROFILE%\.clovent\keys\clovent_vendor_private_key.pem (or .xml)
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var standardDir = Path.Combine(userProfile, ".clovent", "keys");
        var standardPemPath = Path.Combine(standardDir, "clovent_vendor_private_key.pem");
        var standardXmlPath = Path.Combine(standardDir, "clovent_vendor_private_key.xml");

        if (File.Exists(standardPemPath))
        {
            EnsureNotInsideGitRepo(standardPemPath, "Standard external private key");
            Console.WriteLine($"[Key Source] 3) Loaded from standard external path: {standardPemPath}");
            return LoadRsaFromContent(File.ReadAllText(standardPemPath));
        }

        if (File.Exists(standardXmlPath))
        {
            EnsureNotInsideGitRepo(standardXmlPath, "Standard external private key");
            Console.WriteLine($"[Key Source] 3) Loaded from standard external path: {standardXmlPath}");
            return LoadRsaFromContent(File.ReadAllText(standardXmlPath));
        }

        throw new InvalidOperationException(
            "Vendor private signing key could not be located.\nChecked locations:\n" +
            "  1. Explicit --key-file argument\n" +
            "  2. Environment variable CLOVENT_LICENSE_SIGNING_KEY\n" +
            $"  3. Standard external path: {standardPemPath}\n\n" +
            "Please generate keys using 'dotnet run --project tools\\LicenseIssuer -- generate-keys' or configure an external key.");
    }

    private static RSA LoadRsaFromContent(string content)
    {
        var trimmed = content.Trim();
        var rsa = RSA.Create();
        try
        {
            if (trimmed.StartsWith("<", StringComparison.Ordinal))
            {
                rsa.FromXmlString(trimmed);
            }
            else
            {
                rsa.ImportFromPem(trimmed);
            }
            return rsa;
        }
        catch (Exception ex)
        {
            rsa.Dispose();
            throw new InvalidOperationException($"Failed to import RSA private key: {ex.Message}", ex);
        }
    }

    private static int HandleGenerateKeys(Dictionary<string, string> options)
    {
        options.TryGetValue("out", out var outPath);
        if (string.IsNullOrWhiteSpace(outPath))
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            outPath = Path.Combine(userProfile, ".clovent", "keys");
        }

        var fullOutPath = Path.GetFullPath(outPath);
        EnsureNotInsideGitRepo(fullOutPath, "Target key output directory");

        if (!Directory.Exists(fullOutPath))
        {
            Directory.CreateDirectory(fullOutPath);
        }

        Console.WriteLine("================================================================================");
        Console.WriteLine("Clovent Cryptographic Key Generation (RSA-2048)");
        Console.WriteLine("================================================================================");
        Console.WriteLine($"Target Directory: {fullOutPath}");
        Console.WriteLine($"Key ID:           {KeyId}");
        Console.WriteLine("Revoked Keys:     clovent-2026-v1 (REVOKED)");
        Console.WriteLine();

        using var rsa = RSA.Create(2048);

        var privatePem = rsa.ExportPkcs8PrivateKeyPem();
        var privateXml = rsa.ToXmlString(true);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var publicXml = rsa.ToXmlString(false);

        var privatePemFile = Path.Combine(fullOutPath, "clovent_vendor_private_key.pem");
        var privateXmlFile = Path.Combine(fullOutPath, "clovent_vendor_private_key.xml");
        var publicPemFile = Path.Combine(fullOutPath, "clovent_public_key.pem");
        var publicXmlFile = Path.Combine(fullOutPath, "clovent_public_key.xml");
        var metaFile = Path.Combine(fullOutPath, "key_metadata.json");

        File.WriteAllText(privatePemFile, privatePem, Encoding.UTF8);
        File.WriteAllText(privateXmlFile, privateXml, Encoding.UTF8);
        File.WriteAllText(publicPemFile, publicPem, Encoding.UTF8);
        File.WriteAllText(publicXmlFile, publicXml, Encoding.UTF8);

        var metadata = new
        {
            KeyId = KeyId,
            Algorithm = "RSA-2048",
            HashAlgorithm = "SHA-256",
            Padding = "RSASignaturePadding.Pkcs1",
            CreatedDate = DateTimeOffset.UtcNow,
            Status = "Active",
            RevokedKeys = new[]
            {
                new
                {
                    KeyId = "clovent-2026-v1",
                    Status = "Revoked",
                    Reason = "Cryptographic key rotation and repository hardening."
                }
            }
        };

        File.WriteAllText(metaFile, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[OK] Private key (PEM) saved to: {privatePemFile}");
        Console.WriteLine($"[OK] Private key (XML) saved to: {privateXmlFile}");
        Console.WriteLine($"[OK] Public key (PEM)  saved to: {publicPemFile}");
        Console.WriteLine($"[OK] Public key (XML)  saved to: {publicXmlFile}");
        Console.WriteLine($"[OK] Key metadata     saved to: {metaFile}");
        Console.ResetColor();

        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine("PUBLIC KEY XML (For LicenseKeys.cs):");
        Console.WriteLine(publicXml);
        Console.WriteLine("--------------------------------------------------------------------------------");
        Console.WriteLine("PUBLIC KEY PEM:");
        Console.WriteLine(publicPem);
        Console.WriteLine("================================================================================");

        return 0;
    }
    #endregion

    #region License Issuance
    private static int HandleIssue(Dictionary<string, string> options)
    {
        var missingRequired = new List<string>();
        if (!options.TryGetValue("customer", out var customer) || string.IsNullOrWhiteSpace(customer))
        {
            missingRequired.Add("--customer <name>");
        }

        if (!options.TryGetValue("company", out var company) || string.IsNullOrWhiteSpace(company))
        {
            missingRequired.Add("--company <name>");
        }

        if (!options.TryGetValue("type", out var licenseType) || string.IsNullOrWhiteSpace(licenseType))
        {
            licenseType = "Subscription";
        }

        var isPerpetual = string.Equals(licenseType, "Perpetual", StringComparison.OrdinalIgnoreCase);

        if (!options.ContainsKey("days") && !isPerpetual)
        {
            missingRequired.Add("--days <n>");
        }

        if (!options.ContainsKey("terminals"))
        {
            missingRequired.Add("--terminals <n>");
        }

        if (!options.ContainsKey("branches"))
        {
            missingRequired.Add("--branches <n>");
        }

        if (missingRequired.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Error: Missing required option(s):");
            foreach (var opt in missingRequired)
            {
                Console.Error.WriteLine($"  {opt}");
            }
            Console.ResetColor();
            Console.WriteLine("\nAuthoritative usage example:");
            Console.WriteLine("  dotnet run --project tools\\LicenseIssuer -- issue `");
            Console.WriteLine("    --customer \"CBOS Sandbox Test\" `");
            Console.WriteLine("    --company \"CBOS Sandbox Test\" `");
            Console.WriteLine("    --type Trial `");
            Console.WriteLine("    --days 35 `");
            Console.WriteLine("    --modules POS,BackOffice,Inventory,Catalog,Reporting,Restaurant `");
            Console.WriteLine("    --terminals 5 `");
            Console.WriteLine("    --branches 1 `");
            Console.WriteLine("    --machine-id \"<SANDBOX-HARDWARE-ID>\" `");
            Console.WriteLine("    --out \"D:\\clovent-sandbox.lic\"");
            return 1;
        }

        int days = 0;
        if (options.TryGetValue("days", out var daysStr))
        {
            if (!int.TryParse(daysStr, out days) || days < 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error: '--days' must be a non-negative integer.");
                Console.ResetColor();
                return 1;
            }
        }
        else if (isPerpetual)
        {
            days = 36500; // 100 years
        }

        if (isPerpetual && days == 0)
        {
            days = 36500;
        }

        var modules = new List<string>();
        if (options.TryGetValue("modules", out var modulesStr) && !string.IsNullOrWhiteSpace(modulesStr))
        {
            modules = modulesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }
        else
        {
            modules = new List<string> { "BackOffice", "Catalog", "Inventory", "POS", "Reporting", "Restaurant" };
        }

        int terminals = 0;
        if (options.TryGetValue("terminals", out var terminalsStr))
        {
            if (!int.TryParse(terminalsStr, out terminals) || terminals < 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error: '--terminals' must be a non-negative integer (0 = unlimited).");
                Console.ResetColor();
                return 1;
            }
        }

        int branches = 0;
        if (options.TryGetValue("branches", out var branchesStr))
        {
            if (!int.TryParse(branchesStr, out branches) || branches < 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error: '--branches' must be a non-negative integer (0 = unlimited).");
                Console.ResetColor();
                return 1;
            }
        }

        options.TryGetValue("machine-id", out var machineId);
        if (string.IsNullOrWhiteSpace(machineId))
        {
            machineId = null;
        }

        int? maintenanceDays = null;
        if (options.TryGetValue("maintenance-days", out var maintDaysStr))
        {
            if (int.TryParse(maintDaysStr, out var md) && md >= 0)
            {
                maintenanceDays = md;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error: '--maintenance-days' must be a non-negative integer.");
                Console.ResetColor();
                return 1;
            }
        }

        options.TryGetValue("out", out var outFile);
        if (string.IsNullOrWhiteSpace(outFile))
        {
            outFile = "clovent.lic";
        }

        options.TryGetValue("key-file", out var keyFile);

        using var rsa = ResolveAndLoadPrivateKey(keyFile);

        var nowUtc = DateTimeOffset.UtcNow;
        var issueDate = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, nowUtc.Minute, nowUtc.Second, TimeSpan.Zero);
        var validFrom = issueDate;
        var expiryDate = issueDate.AddDays(days);
        DateTimeOffset? maintenanceExpiry = maintenanceDays.HasValue ? issueDate.AddDays(maintenanceDays.Value) : null;

        var normalizedType = char.ToUpperInvariant(licenseType[0]) + licenseType.Substring(1);

        var license = new IssuedLicense
        {
            KeyId = KeyId,
            Product = "Clovent Business Operating System",
            LicenseId = Guid.NewGuid(),
            CustomerName = customer!.Trim(),
            CompanyName = company!.Trim(),
            LicenseType = normalizedType,
            IssueDate = issueDate,
            ValidFrom = validFrom,
            ExpiryDate = expiryDate,
            MaxTerminals = terminals,
            MaxBranches = branches,
            MaintenanceExpiry = maintenanceExpiry,
            AllowedModules = modules,
            MachineId = machineId?.Trim(),
            Signature = string.Empty
        };

        // Canonical payload for RSA-2048 SHA-256 signing (Requirements 4 & 19):
        // $"{KeyId}|{Product}|{LicenseId:D}|{CustomerName.Trim()}|{CompanyName.Trim()}|{LicenseType.Trim()}|{IssueDate:O}|{ValidFrom:O}|{ExpiryDate:O}|{(MaintenanceExpiry.HasValue ? MaintenanceExpiry.Value.ToString("O") : "")}|{MaxBranches}|{MaxTerminals}|{modules}|{machine}"
        var canonicalPayload = license.GetCanonicalPayload();
        var payloadBytes = Encoding.UTF8.GetBytes(canonicalPayload);
        var signatureBytes = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        license.Signature = Convert.ToBase64String(signatureBytes);

        // Immediate self-verification
        var verified = rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        if (!verified)
        {
            throw new InvalidOperationException("Cryptographic verification of newly generated license signature failed!");
        }

        var fullOutPath = Path.GetFullPath(outFile);
        var outDir = Path.GetDirectoryName(fullOutPath);
        if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        var json = JsonSerializer.Serialize(license, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        File.WriteAllText(fullOutPath, json, Encoding.UTF8);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("================================================================================");
        Console.WriteLine("Clovent Software License Issued Successfully");
        Console.WriteLine("================================================================================");
        Console.ResetColor();
        Console.WriteLine($"License ID:      {license.LicenseId}");
        Console.WriteLine($"Customer:        {license.CustomerName}");
        Console.WriteLine($"Company:         {license.CompanyName}");
        Console.WriteLine($"License Type:    {license.LicenseType}");
        Console.WriteLine($"Issue Date:      {license.IssueDate:yyyy-MM-dd HH:mm:ss 'UTC'}");
        Console.WriteLine($"Expiry Date:     {license.ExpiryDate:yyyy-MM-dd HH:mm:ss 'UTC'} ({days} days)");
        Console.WriteLine($"Terminal Limit:  {(license.TerminalLimit <= 0 ? "Unlimited (0)" : license.TerminalLimit.ToString())}");
        Console.WriteLine($"Branch Limit:    {(license.BranchLimit <= 0 ? "Unlimited (0)" : license.BranchLimit.ToString())}");
        if (maintenanceExpiry.HasValue)
        {
            Console.WriteLine($"Maintenance:     Until {maintenanceExpiry.Value:yyyy-MM-dd HH:mm:ss 'UTC'} ({maintenanceDays} days)");
        }
        Console.WriteLine($"Machine Binding: {license.MachineId ?? "(Node-locking disabled - runs on any machine)"}");
        Console.WriteLine($"Allowed Modules: {string.Join(", ", license.AllowedModules)}");
        Console.WriteLine($"Output File:     {fullOutPath}");
        Console.WriteLine($"Signature:       {license.Signature.Substring(0, Math.Min(40, license.Signature.Length))}... ({license.Signature.Length} base64 chars)");
        Console.WriteLine("================================================================================");

        return 0;
    }
    #endregion

    #region License Verification
    private static int HandleVerify(Dictionary<string, string> options)
    {
        if (!options.TryGetValue("license", out var licensePath) || string.IsNullOrWhiteSpace(licensePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Error: Missing required option '--license <file.lic>'.");
            Console.ResetColor();
            return 1;
        }

        if (!File.Exists(licensePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: License file not found: {licensePath}");
            Console.ResetColor();
            return 1;
        }

        var json = File.ReadAllText(licensePath);
        var license = JsonSerializer.Deserialize<IssuedLicense>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (license == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine("Error: Could not deserialize license file.");
            Console.ResetColor();
            return 1;
        }

        using var rsa = RSA.Create();
        if (options.TryGetValue("public-key", out var pubKeySource))
        {
            if (File.Exists(pubKeySource))
            {
                var content = File.ReadAllText(pubKeySource).Trim();
                if (content.StartsWith("<"))
                {
                    rsa.FromXmlString(content);
                }
                else
                {
                    rsa.ImportFromPem(content);
                }
            }
            else if (pubKeySource.Trim().StartsWith("<"))
            {
                rsa.FromXmlString(pubKeySource.Trim());
            }
            else
            {
                rsa.ImportFromPem(pubKeySource.Trim());
            }
        }
        else
        {
            // Default: check standard external public key
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var pubPem = Path.Combine(userProfile, ".clovent", "keys", "clovent_public_key.pem");
            var pubXml = Path.Combine(userProfile, ".clovent", "keys", "clovent_public_key.xml");

            if (File.Exists(pubPem))
            {
                rsa.ImportFromPem(File.ReadAllText(pubPem));
            }
            else if (File.Exists(pubXml))
            {
                rsa.FromXmlString(File.ReadAllText(pubXml));
            }
            else
            {
                throw new FileNotFoundException("No public key specified and default public key not found in %USERPROFILE%\\.clovent\\keys\\");
            }
        }

        var canonicalPayload = license.GetCanonicalPayload();
        var payloadBytes = Encoding.UTF8.GetBytes(canonicalPayload);
        var signatureBytes = Convert.FromBase64String(license.Signature);
        var isValid = rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        if (isValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[VALID] License cryptographic digital signature is VALID.");
            Console.ResetColor();
            Console.WriteLine($"License ID:  {license.LicenseId}");
            Console.WriteLine($"Customer:    {license.CustomerName} ({license.CompanyName})");
            Console.WriteLine($"Type:        {license.LicenseType}");
            Console.WriteLine($"Expiry:      {license.ExpiryDate:yyyy-MM-dd HH:mm:ss 'UTC'}");
            Console.WriteLine($"Modules:     {string.Join(", ", license.AllowedModules)}");
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[INVALID] License digital signature verification FAILED.");
            Console.ResetColor();
            return 1;
        }
    }
    #endregion
}

public sealed class IssuedLicense
{
    public string KeyId { get; set; } = "clovent-2026-v2";

    public string Product { get; set; } = "Clovent Business Operating System";

    public Guid LicenseId { get; set; } = Guid.NewGuid();

    public string CustomerName { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string LicenseType { get; set; } = "Subscription";

    public DateTimeOffset IssueDate { get; set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset ExpiryDate { get; set; }

    public int MaxTerminals { get; set; } = 1; // 0 = unlimited

    public int MaxBranches { get; set; } = 1; // 0 = unlimited

    [JsonIgnore]
    public int TerminalLimit
    {
        get => MaxTerminals;
        set => MaxTerminals = value;
    }

    [JsonIgnore]
    public int BranchLimit
    {
        get => MaxBranches;
        set => MaxBranches = value;
    }

    public DateTimeOffset? MaintenanceExpiry { get; set; }

    [JsonIgnore]
    public DateTimeOffset? MaintenanceExpiryDate
    {
        get => MaintenanceExpiry;
        set => MaintenanceExpiry = value;
    }

    public List<string> AllowedModules { get; set; } = new();

    public string? MachineId { get; set; }

    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Computes the canonical UTF-8 payload representation for cryptographic signing and verification.
    /// Matches Clovent.Desktop.Licensing.CloventLicense.GetCanonicalPayload() specification.
    /// </summary>
    public string GetCanonicalPayload()
    {
        var modules = AllowedModules != null ? string.Join(",", AllowedModules.OrderBy(x => x)) : string.Empty;
        var machine = MachineId?.Trim() ?? string.Empty;
        return $"{KeyId}|{Product}|{LicenseId:D}|{(CustomerName ?? string.Empty).Trim()}|{(CompanyName ?? string.Empty).Trim()}|{(LicenseType ?? string.Empty).Trim()}|{IssueDate:O}|{ValidFrom:O}|{ExpiryDate:O}|{(MaintenanceExpiry.HasValue ? MaintenanceExpiry.Value.ToString("O") : "")}|{MaxBranches}|{MaxTerminals}|{modules}|{machine}";
    }
}
