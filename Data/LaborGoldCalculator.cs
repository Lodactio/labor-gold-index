// Data/LaborGoldCalculator.cs
using System;

namespace Jellyfin.Plugin.LaborGoldIndex.Data;

/// <summary>
/// Core calculation engine for the Labor-Gold Index.
///
/// The Labor-Gold Index (LG) for a given year is defined as:
///
///   G(t) = MedianIncome(t) / GoldPrice(t)
///
/// This represents the number of troy ounces of gold that a median
/// household's annual income could purchase in year t.
///
/// The LG-Adjusted value of a nominal dollar amount is:
///
///   AdjustedValue = NominalValue × (G(t) / G(baseYear))
///
/// This converts nominal dollars into "base-year labor-gold dollars" —
/// a unit that represents permanent purchasing power anchored to both
/// human labor and gold's millennia-spanning store of value.
/// </summary>
public static class LaborGoldCalculator
{
    /// <summary>
    /// Default base year for the index.
    /// </summary>
    public const int DefaultBaseYear = 1920;

    /// <summary>
    /// Calculates G(t) — ounces of gold buyable by median income in year t.
    /// </summary>
    /// <param name="year">The year to calculate for.</param>
    /// <returns>Ounces of gold, or null if data is unavailable.</returns>
    public static decimal? GetGt(int year)
    {
        var income = MedianIncomeData.GetIncome(year);
        var goldPrice = GoldPriceData.GetPrice(year);

        if (income is null || goldPrice is null || goldPrice == 0)
            return null;

        return income.Value / goldPrice.Value;
    }

    /// <summary>
    /// Calculates the LG ratio G(t) / G(baseYear).
    /// Values greater than 1.0 mean labor bought more gold that year than the base year.
    /// Values less than 1.0 mean labor bought less gold.
    /// </summary>
    /// <param name="year">The film's release year.</param>
    /// <param name="baseYear">The anchor year (default 1920).</param>
    /// <returns>The ratio, or null if data is unavailable.</returns>
    public static decimal? GetLaborGoldRatio(int year, int baseYear = DefaultBaseYear)
    {
        var gt = GetGt(year);
        var gBase = GetGt(baseYear);

        if (gt is null || gBase is null || gBase == 0)
            return null;

        return gt.Value / gBase.Value;
    }

    /// <summary>
    /// Converts a nominal dollar value to LG-adjusted dollars.
    /// </summary>
    /// <param name="nominalValue">The nominal value in USD (e.g., box office gross).</param>
    /// <param name="year">The year the value was recorded.</param>
    /// <param name="baseYear">The anchor year (default 1920).</param>
    /// <returns>The LG-adjusted value, or null if data is unavailable.</returns>
    public static decimal? AdjustToLaborGold(
        decimal nominalValue,
        int year,
        int baseYear = DefaultBaseYear)
    {
        var ratio = GetLaborGoldRatio(year, baseYear);
        if (ratio is null) return null;

        return nominalValue * ratio.Value;
    }

    /// <summary>
    /// Calculates the LG-adjusted ROI given budget and gross.
    /// </summary>
    /// <param name="nominalBudget">Production budget in nominal USD.</param>
    /// <param name="nominalGross">Worldwide gross in nominal USD.</param>
    /// <param name="year">Release year.</param>
    /// <param name="baseYear">Anchor year.</param>
    /// <returns>A result containing adjusted values, or null if data is unavailable.</returns>
    public static LaborGoldResult? Calculate(
        decimal nominalBudget,
        decimal nominalGross,
        int year,
        int baseYear = DefaultBaseYear)
    {
        var ratio = GetLaborGoldRatio(year, baseYear);
        var gt = GetGt(year);
        var gBase = GetGt(baseYear);

        if (ratio is null || gt is null || gBase is null)
            return null;

        var adjBudget = nominalBudget * ratio.Value;
        var adjGross = nominalGross * ratio.Value;
        var adjProfit = adjGross - adjBudget;
        var roi = adjBudget > 0 ? (adjProfit / adjBudget) * 100m : 0m;

        return new LaborGoldResult
        {
            Year = year,
            BaseYear = baseYear,
            GoldOzBuyable = gt.Value,
            BaseGoldOzBuyable = gBase.Value,
            LaborGoldRatio = ratio.Value,
            NominalBudget = nominalBudget,
            NominalGross = nominalGross,
            AdjustedBudget = adjBudget,
            AdjustedGross = adjGross,
            AdjustedProfit = adjProfit,
            ReturnOnInvestment = roi
        };
    }
}

/// <summary>
/// Represents the complete Labor-Gold analysis for a film.
/// </summary>
public class LaborGoldResult
{
    /// <summary>Gets or sets the release year.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the base year for the index.</summary>
    public int BaseYear { get; set; }

    /// <summary>Gets or sets oz of gold buyable by median income in release year.</summary>
    public decimal GoldOzBuyable { get; set; }

    /// <summary>Gets or sets oz of gold buyable by median income in base year.</summary>
    public decimal BaseGoldOzBuyable { get; set; }

    /// <summary>Gets or sets the LG ratio (G_t / G_base).</summary>
    public decimal LaborGoldRatio { get; set; }

    /// <summary>Gets or sets the nominal production budget.</summary>
    public decimal NominalBudget { get; set; }

    /// <summary>Gets or sets the nominal worldwide gross.</summary>
    public decimal NominalGross { get; set; }

    /// <summary>Gets or sets the LG-adjusted budget.</summary>
    public decimal AdjustedBudget { get; set; }

    /// <summary>Gets or sets the LG-adjusted worldwide gross.</summary>
    public decimal AdjustedGross { get; set; }

    /// <summary>Gets or sets the LG-adjusted profit (gross - budget).</summary>
    public decimal AdjustedProfit { get; set; }

    /// <summary>Gets or sets the LG-adjusted ROI percentage.</summary>
    public decimal ReturnOnInvestment { get; set; }

    /// <summary>
    /// Formats the adjusted gross for display.
    /// </summary>
    public string FormattedAdjustedGross
    {
        get
        {
            if (AdjustedGross >= 1_000_000_000m)
                return $"${AdjustedGross / 1_000_000_000m:F2}B";
            if (AdjustedGross >= 1_000_000m)
                return $"${AdjustedGross / 1_000_000m:F1}M";
            if (AdjustedGross >= 1_000m)
                return $"${AdjustedGross / 1_000m:F0}K";
            return $"${AdjustedGross:F0}";
        }
    }
}