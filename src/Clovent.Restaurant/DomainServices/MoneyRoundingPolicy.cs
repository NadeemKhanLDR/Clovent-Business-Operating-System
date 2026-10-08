namespace Clovent.Restaurant.DomainServices;

/// <summary>
/// Centralized financial rounding policy for the Clovent Business Operating System (CBOS).
/// Implements the authoritative financial integrity rules governed by AGENTS.md,
/// financial-integrity.md, and PDR-0004.
/// Enforces MidpointRounding.AwayFromZero and 2-decimal transactional currency precision,
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
    /// Rounds a monetary figure to 2 decimal places using the approved MidpointRounding.AwayFromZero policy.
    /// </summary>
    /// <param name="amount">The unrounded monetary figure.</param>
    /// <returns>Exact 2-decimal rounded money amount.</returns>
    public static decimal RoundMoney(decimal amount) =>
        Math.Round(amount, CurrencyDecimals, ApprovedMidpointRounding);

    /// <summary>
    /// Rounds an intermediate or unit price figure to 4 decimal places using AwayFromZero.
    /// </summary>
    /// <param name="unitPrice">The unrounded unit price.</param>
    /// <returns>Exact 4-decimal rounded unit price.</returns>
    public static decimal RoundUnitPrice(decimal unitPrice) =>
        Math.Round(unitPrice, UnitPriceDecimals, ApprovedMidpointRounding);

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
