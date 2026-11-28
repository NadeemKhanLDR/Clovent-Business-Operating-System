namespace Clovent.Restaurant.DomainServices;

/// <summary>
/// Centralized financial rounding policy for the Clovent Business Operating System (CBOS).
/// Implements the authoritative financial integrity rules governed by AGENTS.md,
/// financial-integrity.md, and PDR-0004.
/// Enforces MidpointRounding.AwayFromZero and explicit currency precision,
/// strictly prohibiting ad-hoc Math.Round() invocations across the codebase.
/// </summary>
public static class MoneyRoundingPolicy
{
    /// <summary>Default transactional decimal precision for settled records, drawer totals, and ledgers.</summary>
    public const int CurrencyDecimals = 2;

    /// <summary>Extended decimal precision for intermediate calculations, recipe costs, and fractional unit prices.</summary>
    public const int UnitPriceDecimals = 4;

    /// <summary>Centrally approved midpoint rounding strategy across the platform.</summary>
    public const MidpointRounding ApprovedMidpointRounding = MidpointRounding.AwayFromZero;

    /// <summary>
    /// Rounds a monetary figure to the default 2 decimal places using MidpointRounding.AwayFromZero.
    /// </summary>
    /// <param name="amount">The unrounded monetary figure.</param>
    /// <returns>Exact 2-decimal rounded money amount.</returns>
    public static decimal RoundMoney(decimal amount) =>
        Math.Round(amount, CurrencyDecimals, ApprovedMidpointRounding);

    /// <summary>
    /// Rounds a monetary figure to a currency-specific decimal precision using MidpointRounding.AwayFromZero.
    /// Supports international currencies with 0 (e.g. JPY, KRW), 2 (PKR, USD, EUR), or 3 (BHD, KWD, OMR) decimal places.
    /// </summary>
    /// <param name="amount">The unrounded monetary figure.</param>
    /// <param name="decimals">Number of decimal places specified by the active currency.</param>
    /// <returns>Exact rounded money amount.</returns>
    public static decimal RoundMoney(decimal amount, int decimals) =>
        Math.Round(amount, Math.Clamp(decimals, 0, 4), ApprovedMidpointRounding);

    /// <summary>
    /// Rounds a calculated sales tax amount to the active currency precision using AwayFromZero.
    /// </summary>
    public static decimal RoundTax(decimal taxAmount, int decimals = CurrencyDecimals) =>
        RoundMoney(taxAmount, decimals);

    /// <summary>
    /// Rounds an item- or order-level discount amount using AwayFromZero.
    /// </summary>
    public static decimal RoundDiscount(decimal discountAmount, int decimals = CurrencyDecimals) =>
        RoundMoney(discountAmount, decimals);

    /// <summary>
    /// Rounds a service charge amount using AwayFromZero.
    /// </summary>
    public static decimal RoundServiceCharge(decimal serviceChargeAmount, int decimals = CurrencyDecimals) =>
        RoundMoney(serviceChargeAmount, decimals);

    /// <summary>
    /// Rounds physical cash change due using AwayFromZero.
    /// </summary>
    public static decimal RoundChange(decimal changeAmount, int decimals = CurrencyDecimals) =>
        RoundMoney(changeAmount, decimals);

    /// <summary>
    /// Rounds recorded tender amount using AwayFromZero.
    /// </summary>
    public static decimal RoundTender(decimal tenderAmount, int decimals = CurrencyDecimals) =>
        RoundMoney(tenderAmount, decimals);

    /// <summary>
    /// Rounds an outstanding balance using AwayFromZero.
    /// </summary>
    public static decimal RoundBalance(decimal balanceAmount, int decimals = CurrencyDecimals) =>
        RoundMoney(balanceAmount, decimals);

    /// <summary>
    /// Rounds an intermediate or unit price figure to 4 decimal places (or specified decimals) using AwayFromZero.
    /// </summary>
    /// <param name="unitPrice">The unrounded unit price.</param>
    /// <param name="decimals">Target decimal precision (defaults to 4).</param>
    /// <returns>Exact rounded unit price.</returns>
    public static decimal RoundUnitPrice(decimal unitPrice, int decimals = UnitPriceDecimals) =>
        Math.Round(unitPrice, Math.Clamp(decimals, 0, 6), ApprovedMidpointRounding);

    /// <summary>
    /// Rounds any decimal quantity or intermediate value to the specified number of decimal places
    /// using the centralized MidpointRounding.AwayFromZero strategy.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="decimals">Number of decimal places.</param>
    /// <returns>Rounded value.</returns>
    public static decimal Round(decimal value, int decimals) =>
        Math.Round(value, decimals, ApprovedMidpointRounding);
}
