using System.Text.RegularExpressions;

namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Utility for sanitizing database error messages and connection strings.
/// Ensures plain text passwords, credentials, and sensitive connection details are never leaked.
/// </summary>
public static class DatabaseErrorMasker
{
    private static readonly Regex PasswordKeyValuePattern = new(
        @"(?i)(password|pwd|user id|uid)\s*=\s*([^;]+)",
        RegexOptions.Compiled);

    /// <summary>
    /// Masks sensitive parts of an error message, removing any occurrences of the provided password
    /// or connection string parameters.
    /// </summary>
    /// <param name="rawMessage">The original exception or log message.</param>
    /// <param name="sensitiveInput">An optional password or raw connection string to strip from the message.</param>
    /// <returns>A sanitized string safe for user display or persistent logging.</returns>
    public static string Mask(string? rawMessage, string? sensitiveInput = null)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            return string.Empty;
        }

        var sanitized = rawMessage;

        if (!string.IsNullOrWhiteSpace(sensitiveInput))
        {
            if (sensitiveInput.Contains('='))
            {
                var matches = PasswordKeyValuePattern.Matches(sensitiveInput);
                foreach (Match match in matches)
                {
                    if (match.Groups[2].Length > 0)
                    {
                        var secretValue = match.Groups[2].Value.Trim();
                        if (secretValue.Length > 0)
                        {
                            sanitized = sanitized.Replace(secretValue, "******", StringComparison.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            else
            {
                sanitized = sanitized.Replace(sensitiveInput, "******", StringComparison.OrdinalIgnoreCase);
            }
        }

        // Mask any inline key-value pairs matching Password= or Pwd=
        sanitized = Regex.Replace(sanitized, @"(?i)(password|pwd)\s*=\s*([^;]+)", "$1=******");

        return sanitized;
    }
}
