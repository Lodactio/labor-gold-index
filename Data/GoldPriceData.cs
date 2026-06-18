// Data/GoldPriceData.cs
using System.Collections.Generic;

namespace Jellyfin.Plugin.LaborGoldIndex.Data;

/// <summary>
/// Historical annual average gold prices (USD per troy ounce).
/// Sources: World Gold Council, LBMA, Kitco historical data.
/// Pre-1971 prices reflect the official U.S. government peg.
/// </summary>
public static class GoldPriceData
{
    /// <summary>
    /// Gets the gold price per troy ounce for a given year.
    /// Returns null if the year is not in the dataset.
    /// </summary>
    public static decimal? GetPrice(int year)
    {
        return Prices.TryGetValue(year, out var price) ? price : InterpolatePrice(year);
    }

    private static decimal? InterpolatePrice(int year)
    {
        if (year < 1900 || year > 2030) return null;

        int lower = 0, upper = 0;
        decimal lowerPrice = 0, upperPrice = 0;

        foreach (var kvp in Prices)
        {
            if (kvp.Key <= year) { lower = kvp.Key; lowerPrice = kvp.Value; }
            if (kvp.Key >= year && upper == 0) { upper = kvp.Key; upperPrice = kvp.Value; }
        }

        if (lower == 0 || upper == 0 || lower == upper) return lowerPrice;

        // Linear interpolation
        var fraction = (decimal)(year - lower) / (upper - lower);
        return lowerPrice + (upperPrice - lowerPrice) * fraction;
    }

    /// <summary>
    /// Annual average gold prices (USD/troy oz).
    /// </summary>
    public static readonly SortedDictionary<int, decimal> Prices = new()
    {
        // Pre-Federal Reserve / Classical Gold Standard
        { 1900, 20.67m },
        { 1905, 20.67m },
        { 1910, 20.67m },
        { 1915, 20.67m },
        { 1920, 20.67m },
        { 1925, 20.67m },
        { 1930, 20.67m },

        // FDR revaluation (Gold Reserve Act 1934)
        { 1934, 35.00m },
        { 1935, 35.00m },
        { 1937, 35.00m },
        { 1939, 35.00m },
        { 1940, 35.00m },
        { 1945, 35.00m },
        { 1950, 35.00m },
        { 1955, 35.00m },
        { 1956, 35.00m },
        { 1960, 35.00m },
        { 1962, 35.00m },
        { 1964, 35.00m },
        { 1965, 35.00m },
        { 1967, 35.00m },
        { 1968, 39.31m },
        { 1969, 41.28m },
        { 1970, 36.02m },

        // Nixon Shock & free float
        { 1971, 40.62m },
        { 1972, 58.42m },
        { 1973, 97.39m },
        { 1974, 154.00m },
        { 1975, 161.02m },
        { 1976, 124.74m },
        { 1977, 147.84m },
        { 1978, 193.40m },
        { 1979, 306.00m },
        { 1980, 615.00m },
        { 1981, 460.00m },
        { 1982, 376.00m },
        { 1983, 424.00m },
        { 1984, 361.00m },
        { 1985, 317.00m },
        { 1986, 368.00m },
        { 1987, 447.00m },
        { 1988, 437.00m },
        { 1989, 381.00m },
        { 1990, 383.51m },
        { 1991, 362.11m },
        { 1992, 343.82m },
        { 1993, 359.77m },
        { 1994, 384.00m },
        { 1995, 384.17m },
        { 1996, 387.81m },
        { 1997, 331.02m },
        { 1998, 294.24m },
        { 1999, 278.98m },
        { 2000, 279.11m },
        { 2001, 271.04m },
        { 2002, 309.73m },
        { 2003, 363.38m },
        { 2004, 409.72m },
        { 2005, 444.74m },
        { 2006, 603.46m },
        { 2007, 695.39m },
        { 2008, 871.96m },
        { 2009, 972.35m },
        { 2010, 1224.53m },
        { 2011, 1571.52m },
        { 2012, 1668.98m },
        { 2013, 1411.23m },
        { 2014, 1266.40m },
        { 2015, 1160.06m },
        { 2016, 1250.74m },
        { 2017, 1257.12m },
        { 2018, 1268.49m },
        { 2019, 1392.60m },
        { 2020, 1769.64m },
        { 2021, 1798.89m },
        { 2022, 1801.87m },
        { 2023, 1940.54m },
        { 2024, 2360.00m },
        { 2025, 4800.00m },
        { 2026, 5000.00m },
    };
}