namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// The payment method the cashier last used, persisted as a plain text file
/// under the current user's local application data folder - the same
/// file-convention-not-a-database-column choice
/// <c>Forms.Base.Localization.LanguagePreferenceStore</c> already establishes
/// for a Desktop-local preference nothing outside this process needs to know
/// about. Read when the POS payment buttons are first built so the last-used
/// (i.e. configured) method is pre-selected on every new order, and written
/// whenever the cashier picks a method. The cashier can still freely select
/// any other method at any time.
/// </summary>
internal static class PosPaymentMethodPreferenceStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "pos-payment-method.txt");

    /// <summary>Reads the persisted payment method id, or <see langword="null"/> if none has been saved yet.</summary>
    public static Guid? Load()
    {
        try
        {
            return File.Exists(FilePath) && Guid.TryParse(File.ReadAllText(FilePath).Trim(), out var id) ? id : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>Persists <paramref name="paymentMethodId"/> as the default payment method for future POS sessions.</summary>
    public static void Save(Guid paymentMethodId)
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(directory);
            File.WriteAllText(FilePath, paymentMethodId.ToString());
        }
        catch (IOException)
        {
            // A read-only profile or full disk must never break tendering - the
            // choice simply won't persist to the next session.
        }
    }
}
