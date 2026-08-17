using System.Text.RegularExpressions;
using Clovent.Domain;

namespace Clovent.Desktop.Startup;

/// <summary>
/// Turns an exception into text a user (not a developer) can act on -
/// "Never show technical exceptions... Problem / Reason / Solution" is the
/// standing rule for every screen, and <see cref="ErrorDialogForm"/> is the
/// single funnel every unhandled exception in the process passes through
/// (see <see cref="GlobalExceptionHandler"/>), so fixing the summary text
/// here protects every screen at once rather than requiring a bespoke catch
/// block per action. The full, untouched exception (including any raw
/// identifier this class strips from the summary) stays available behind
/// "Show Details" for support/logs.
/// </summary>
public static class FriendlyErrorText
{
    // Matches a bare GUID, optionally single-quoted (every domain exception
    // in this solution interpolates a strongly-typed id - OrderId, TableId,
    // etc. - as "'{guid}'" via its own ToString() override) - stripped
    // because a cashier reading "Order '3fa85f64-...' cannot be voided" has
    // no use for that identifier, only for what it's telling them.
    private static readonly Regex QuotedGuidPattern = new(
        @"\s*'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}'",
        RegexOptions.Compiled);

    private static readonly Regex BareGuidPattern = new(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        RegexOptions.Compiled);

    /// <summary>Builds a user-facing summary for <paramref name="exception"/>.</summary>
    public static string Summarize(Exception exception)
    {
        // Every bounded context's own DomainException (RestaurantDomainException,
        // CatalogDomainException, ...) already writes a plain-English,
        // one-sentence rule violation - "Order cannot be voided while
        // Completed." - the only technical leak is an embedded raw id, so
        // stripping that (rather than replacing the whole message with a
        // generic one) keeps the specific, actionable reason a user needs.
        if (exception is DomainException)
        {
            return StripIdentifiers(exception.Message);
        }

        // Every bounded context also has its own sealed NotFoundException
        // type (one per Application project, by design - see each project's
        // own NotFoundException.cs) - matched by name rather than a shared
        // base type, since none exists across project boundaries.
        if (exception.GetType().Name == "NotFoundException")
        {
            return "The item you were working with could not be found. It may have been deleted, or changed on another screen.\nTry refreshing the screen and starting again.";
        }

        // A genuinely unexpected/technical failure (a null reference, a
        // database timeout, ...) - no domain-specific reason to give, so the
        // message stays generic but still names a concrete next step rather
        // than a raw exception type name.
        return "Something went wrong and this action could not be completed.\nTry again. If this keeps happening, click \"Show Details\" below and share that information with support.";
    }

    private static string StripIdentifiers(string message)
    {
        var withoutQuoted = QuotedGuidPattern.Replace(message, string.Empty);
        var withoutBare = BareGuidPattern.Replace(withoutQuoted, string.Empty);
        return Regex.Replace(withoutBare, @"[ \t]{2,}", " ").Trim();
    }
}
