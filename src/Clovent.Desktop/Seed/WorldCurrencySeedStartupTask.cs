using Clovent.MasterData.Currencies;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.Platform.Bootstrap;

namespace Clovent.Desktop.Seed;

/// <summary>
/// Ensures the currency catalog contains the standard ISO-4217 world
/// currencies so administrators pick from a master list (and "Set as
/// Default") instead of hand-typing codes, names, and symbols for
/// well-known currencies like PKR or USD. Idempotent: only codes that do
/// not exist yet are inserted, and existing records - including any custom
/// currencies a business created itself - are never modified or deleted,
/// so historical transaction data keeps pointing at unchanged rows. Unlike
/// the <c>Development*SeedStartupTask</c> family this is production
/// reference data, not a dev convenience, so it runs unconditionally on
/// every startup.
/// </summary>
public sealed class WorldCurrencySeedStartupTask(
    ICurrencyRepository currencyRepository,
    MasterDataDbContext masterDataDbContext) : IStartupTask
{
    // (ISO-4217 code, name, symbol, minor-unit digits) for the world's
    // circulating currencies.
    private static readonly (string Code, string Name, string Symbol, int DecimalPlaces)[] WorldCurrencies =
    [
        ("AED", "UAE Dirham", "د.إ", 2),
        ("AFN", "Afghan Afghani", "؋", 2),
        ("ALL", "Albanian Lek", "L", 2),
        ("AMD", "Armenian Dram", "֏", 2),
        ("ANG", "Netherlands Antillean Guilder", "ƒ", 2),
        ("AOA", "Angolan Kwanza", "Kz", 2),
        ("ARS", "Argentine Peso", "$", 2),
        ("AUD", "Australian Dollar", "$", 2),
        ("AWG", "Aruban Florin", "ƒ", 2),
        ("AZN", "Azerbaijani Manat", "₼", 2),
        ("BAM", "Bosnia-Herzegovina Convertible Mark", "KM", 2),
        ("BBD", "Barbadian Dollar", "$", 2),
        ("BDT", "Bangladeshi Taka", "৳", 2),
        ("BGN", "Bulgarian Lev", "лв", 2),
        ("BHD", "Bahraini Dinar", "BD", 3),
        ("BIF", "Burundian Franc", "FBu", 0),
        ("BMD", "Bermudian Dollar", "$", 2),
        ("BND", "Brunei Dollar", "$", 2),
        ("BOB", "Bolivian Boliviano", "Bs", 2),
        ("BRL", "Brazilian Real", "R$", 2),
        ("BSD", "Bahamian Dollar", "$", 2),
        ("BTN", "Bhutanese Ngultrum", "Nu.", 2),
        ("BWP", "Botswanan Pula", "P", 2),
        ("BYN", "Belarusian Ruble", "Br", 2),
        ("BZD", "Belize Dollar", "BZ$", 2),
        ("CAD", "Canadian Dollar", "$", 2),
        ("CDF", "Congolese Franc", "FC", 2),
        ("CHF", "Swiss Franc", "CHF", 2),
        ("CLP", "Chilean Peso", "$", 0),
        ("CNY", "Chinese Yuan", "¥", 2),
        ("COP", "Colombian Peso", "$", 2),
        ("CRC", "Costa Rican Colón", "₡", 2),
        ("CUP", "Cuban Peso", "$", 2),
        ("CVE", "Cape Verdean Escudo", "$", 2),
        ("CZK", "Czech Koruna", "Kč", 2),
        ("DJF", "Djiboutian Franc", "Fdj", 0),
        ("DKK", "Danish Krone", "kr", 2),
        ("DOP", "Dominican Peso", "RD$", 2),
        ("DZD", "Algerian Dinar", "دج", 2),
        ("EGP", "Egyptian Pound", "E£", 2),
        ("ERN", "Eritrean Nakfa", "Nfk", 2),
        ("ETB", "Ethiopian Birr", "Br", 2),
        ("EUR", "Euro", "€", 2),
        ("FJD", "Fijian Dollar", "$", 2),
        ("FKP", "Falkland Islands Pound", "£", 2),
        ("GBP", "British Pound", "£", 2),
        ("GEL", "Georgian Lari", "₾", 2),
        ("GHS", "Ghanaian Cedi", "₵", 2),
        ("GIP", "Gibraltar Pound", "£", 2),
        ("GMD", "Gambian Dalasi", "D", 2),
        ("GNF", "Guinean Franc", "FG", 0),
        ("GTQ", "Guatemalan Quetzal", "Q", 2),
        ("GYD", "Guyanaese Dollar", "$", 2),
        ("HKD", "Hong Kong Dollar", "HK$", 2),
        ("HNL", "Honduran Lempira", "L", 2),
        ("HRK", "Croatian Kuna", "kn", 2),
        ("HTG", "Haitian Gourde", "G", 2),
        ("HUF", "Hungarian Forint", "Ft", 2),
        ("IDR", "Indonesian Rupiah", "Rp", 2),
        ("ILS", "Israeli New Shekel", "₪", 2),
        ("INR", "Indian Rupee", "₹", 2),
        ("IQD", "Iraqi Dinar", "ع.د", 3),
        ("IRR", "Iranian Rial", "﷼", 2),
        ("ISK", "Icelandic Króna", "kr", 0),
        ("JMD", "Jamaican Dollar", "J$", 2),
        ("JOD", "Jordanian Dinar", "JD", 3),
        ("JPY", "Japanese Yen", "¥", 0),
        ("KES", "Kenyan Shilling", "KSh", 2),
        ("KGS", "Kyrgystani Som", "с", 2),
        ("KHR", "Cambodian Riel", "៛", 2),
        ("KMF", "Comorian Franc", "CF", 0),
        ("KRW", "South Korean Won", "₩", 0),
        ("KWD", "Kuwaiti Dinar", "KD", 3),
        ("KYD", "Cayman Islands Dollar", "$", 2),
        ("KZT", "Kazakhstani Tenge", "₸", 2),
        ("LAK", "Laotian Kip", "₭", 2),
        ("LBP", "Lebanese Pound", "ل.ل", 2),
        ("LKR", "Sri Lankan Rupee", "Rs", 2),
        ("LRD", "Liberian Dollar", "$", 2),
        ("LSL", "Lesotho Loti", "L", 2),
        ("LYD", "Libyan Dinar", "ل.د", 3),
        ("MAD", "Moroccan Dirham", "د.م.", 2),
        ("MDL", "Moldovan Leu", "L", 2),
        ("MGA", "Malagasy Ariary", "Ar", 2),
        ("MKD", "Macedonian Denar", "ден", 2),
        ("MMK", "Myanmar Kyat", "K", 2),
        ("MNT", "Mongolian Tugrik", "₮", 2),
        ("MOP", "Macanese Pataca", "MOP$", 2),
        ("MRU", "Mauritanian Ouguiya", "UM", 2),
        ("MUR", "Mauritian Rupee", "₨", 2),
        ("MVR", "Maldivian Rufiyaa", "Rf", 2),
        ("MWK", "Malawian Kwacha", "MK", 2),
        ("MXN", "Mexican Peso", "$", 2),
        ("MYR", "Malaysian Ringgit", "RM", 2),
        ("MZN", "Mozambican Metical", "MT", 2),
        ("NAD", "Namibian Dollar", "$", 2),
        ("NGN", "Nigerian Naira", "₦", 2),
        ("NIO", "Nicaraguan Córdoba", "C$", 2),
        ("NOK", "Norwegian Krone", "kr", 2),
        ("NPR", "Nepalese Rupee", "₨", 2),
        ("NZD", "New Zealand Dollar", "$", 2),
        ("OMR", "Omani Rial", "ر.ع.", 3),
        ("PAB", "Panamanian Balboa", "B/.", 2),
        ("PEN", "Peruvian Sol", "S/", 2),
        ("PGK", "Papua New Guinean Kina", "K", 2),
        ("PHP", "Philippine Peso", "₱", 2),
        ("PKR", "Pakistani Rupee", "Rs.", 2),
        ("PLN", "Polish Zloty", "zł", 2),
        ("PYG", "Paraguayan Guarani", "₲", 0),
        ("QAR", "Qatari Riyal", "ر.ق", 2),
        ("RON", "Romanian Leu", "lei", 2),
        ("RSD", "Serbian Dinar", "дин", 2),
        ("RUB", "Russian Ruble", "₽", 2),
        ("RWF", "Rwandan Franc", "FRw", 0),
        ("SAR", "Saudi Riyal", "ر.س", 2),
        ("SBD", "Solomon Islands Dollar", "$", 2),
        ("SCR", "Seychellois Rupee", "₨", 2),
        ("SDG", "Sudanese Pound", "ج.س.", 2),
        ("SEK", "Swedish Krona", "kr", 2),
        ("SGD", "Singapore Dollar", "$", 2),
        ("SHP", "Saint Helena Pound", "£", 2),
        ("SLE", "Sierra Leonean Leone", "Le", 2),
        ("SOS", "Somalian Shilling", "Sh", 2),
        ("SRD", "Surinamese Dollar", "$", 2),
        ("SSP", "South Sudanese Pound", "£", 2),
        ("STN", "São Tomé & Príncipe Dobra", "Db", 2),
        ("SVC", "Salvadoran Colón", "₡", 2),
        ("SYP", "Syrian Pound", "£S", 2),
        ("SZL", "Swazi Lilangeni", "E", 2),
        ("THB", "Thai Baht", "฿", 2),
        ("TJS", "Tajikistani Somoni", "SM", 2),
        ("TMT", "Turkmenistani Manat", "m", 2),
        ("TND", "Tunisian Dinar", "د.ت", 3),
        ("TOP", "Tongan Pa'anga", "T$", 2),
        ("TRY", "Turkish Lira", "₺", 2),
        ("TTD", "Trinidad & Tobago Dollar", "TT$", 2),
        ("TWD", "New Taiwan Dollar", "NT$", 2),
        ("TZS", "Tanzanian Shilling", "TSh", 2),
        ("UAH", "Ukrainian Hryvnia", "₴", 2),
        ("UGX", "Ugandan Shilling", "USh", 0),
        ("USD", "US Dollar", "$", 2),
        ("UYU", "Uruguayan Peso", "$U", 2),
        ("UZS", "Uzbekistani Som", "лв", 2),
        ("VES", "Venezuelan Bolívar", "Bs", 2),
        ("VND", "Vietnamese Dong", "₫", 0),
        ("VUV", "Vanuatu Vatu", "VT", 0),
        ("WST", "Samoan Tala", "WS$", 2),
        ("XAF", "Central African CFA Franc", "FCFA", 0),
        ("XCD", "East Caribbean Dollar", "$", 2),
        ("XOF", "West African CFA Franc", "CFA", 0),
        ("XPF", "CFP Franc", "₣", 0),
        ("YER", "Yemeni Rial", "﷼", 2),
        ("ZAR", "South African Rand", "R", 2),
        ("ZMW", "Zambian Kwacha", "ZK", 2),
        ("ZWL", "Zimbabwean Dollar", "$", 2),
    ];

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var existingCodes = (await currencyRepository.GetAllAsync(cancellationToken))
            .Select(c => c.Code.Value)
            .ToHashSet(StringComparer.Ordinal);

        var missing = WorldCurrencies.Where(c => !existingCodes.Contains(c.Code)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        foreach (var (code, name, symbol, decimalPlaces) in missing)
        {
            await currencyRepository.AddAsync(
                Currency.Create(CurrencyCode.Create(code), name, symbol, decimalPlaces),
                cancellationToken);
        }

        await masterDataDbContext.SaveChangesAsync(cancellationToken);
    }
}
