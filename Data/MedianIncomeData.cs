// Data/MedianIncomeData.cs
using System.Collections.Generic;

namespace Jellyfin.Plugin.LaborGoldIndex.Data;

/// <summary>
/// U.S. Median Household Income (nominal USD).
/// Sources: U.S. Census Bureau, Historical Statistics of the United States,
/// Federal Reserve Economic Data (FRED).
/// Pre-1950 figures are estimates based on BLS and NBER data.
/// </summary>
public static class MedianIncomeData
{
    /// <summary>
    /// Gets the median household income for a given year.
    /// Returns null if the year cannot be determined.
    /// </summary>
    public static decimal? GetIncome(int year)
    {
        return Incomes.TryGetValue(year, out var income) ? income : InterpolateIncome(year);
    }

    private static decimal? InterpolateIncome(int year)
    {
        if (year < 1900 || year > 2030) return null;

        int lower = 0, upper = 0;
        decimal lowerIncome = 0, upperIncome = 0;

        foreach (var kvp in Incomes)
        {
            if (kvp.Key <= year) { lower = kvp.Key; lowerIncome = kvp.Value; }
            if (kvp.Key >= year && upper == 0) { upper = kvp.Key; upperIncome = kvp.Value; }
        }

        if (lower == 0 || upper == 0 || lower == upper) return lowerIncome;

        var fraction = (decimal)(year - lower) / (upper - lower);
        return lowerIncome + (upperIncome - lowerIncome) * fraction;
    }

    /// <summary>
    /// Median household income by year (nominal USD).
    /// </summary>
    public static readonly SortedDictionary<int, decimal> Incomes = new()
    {
        // Estimates from BLS / Historical Statistics of the US
        { 1900, 750m },
        { 1905, 800m },
        { 1910, 900m },
        { 1915, 1_050m },
        { 1920, 1_400m },
        { 1925, 1_500m },
        { 1930, 1_388m },
        { 1933, 1_000m },
        { 1935, 1_100m },
        { 1937, 1_200m },
        { 1939, 1_315m },
        { 1940, 1_368m },
        { 1941, 1_750m },
        { 1943, 2_350m },
        { 1945, 2_595m },
        { 1947, 3_031m },
        { 1950, 3_300m },
        { 1953, 3_800m },
        { 1955, 4_400m },
        { 1956, 4_700m },
        { 1958, 5_100m },
        { 1960, 5_600m },
        { 1962, 5_956m },
        { 1964, 6_569m },
        { 1965, 6_900m },
        { 1967, 7_143m },
        { 1968, 8_937m },
        { 1969, 9_433m },
        { 1970, 9_870m },
        { 1971, 10_290m },
        { 1972, 11_120m },
        { 1973, 12_050m },
        { 1974, 12_900m },
        { 1975, 12_686m },
        { 1976, 13_500m },
        { 1977, 14_058m },
        { 1978, 15_064m },
        { 1979, 16_461m },
        { 1980, 17_710m },
        { 1981, 19_074m },
        { 1982, 20_171m },
        { 1983, 20_885m },
        { 1984, 22_415m },
        { 1985, 23_620m },
        { 1986, 24_900m },
        { 1987, 26_061m },
        { 1988, 27_225m },
        { 1989, 28_906m },
        { 1990, 29_940m },
        { 1991, 30_126m },
        { 1992, 30_636m },
        { 1993, 31_241m },
        { 1994, 32_264m },
        { 1995, 34_076m },
        { 1996, 35_492m },
        { 1997, 37_005m },
        { 1998, 38_885m },
        { 1999, 40_696m },
        { 2000, 41_990m },
        { 2001, 42_228m },
        { 2002, 42_409m },
        { 2003, 43_318m },
        { 2004, 44_334m },
        { 2005, 46_326m },
        { 2006, 48_201m },
        { 2007, 50_233m },
        { 2008, 50_303m },
        { 2009, 49_777m },
        { 2010, 49_276m },
        { 2011, 50_054m },
        { 2012, 51_017m },
        { 2013, 51_939m },
        { 2014, 53_657m },
        { 2015, 56_516m },
        { 2016, 59_039m },
        { 2017, 61_372m },
        { 2018, 63_179m },
        { 2019, 68_703m },
        { 2020, 67_521m },
        { 2021, 70_784m },
        { 2022, 74_580m },
        { 2023, 74_580m },
        { 2024, 74_580m },
        { 2025, 76_000m },
        { 2026, 78_000m },
    };
}