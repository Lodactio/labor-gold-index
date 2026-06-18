// Api/LaborGoldController.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Plugin.LaborGoldIndex.Data;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.LaborGoldIndex.Api;

/// <summary>
/// API controller for Labor-Gold Index queries.
/// </summary>
[ApiController]
[Route("LaborGold")]
[Authorize]
public class LaborGoldController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaborGoldController"/> class.
    /// </summary>
    public LaborGoldController(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Calculate the LG-adjusted value for arbitrary inputs.
    /// </summary>
    /// <param name="nominalValue">Nominal dollar amount.</param>
    /// <param name="year">The year of the value.</param>
    /// <param name="baseYear">Base year (default 1920).</param>
    /// <returns>The LG-adjusted value.</returns>
    [HttpGet("Calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<object> Calculate(
        [Required] decimal nominalValue,
        [Required] int year,
        int baseYear = 1920)
    {
        var adjusted = LaborGoldCalculator.AdjustToLaborGold(nominalValue, year, baseYear);
        var gt = LaborGoldCalculator.GetGt(year);
        var gBase = LaborGoldCalculator.GetGt(baseYear);
        var ratio = LaborGoldCalculator.GetLaborGoldRatio(year, baseYear);

        if (adjusted is null)
            return BadRequest(new { Error = $"No data available for year {year}" });

        return Ok(new
        {
            NominalValue = nominalValue,
            Year = year,
            BaseYear = baseYear,
            GoldOzBuyableInYear = gt,
            GoldOzBuyableInBaseYear = gBase,
            LaborGoldRatio = ratio,
            AdjustedValue = adjusted,
            Interpretation = $"${nominalValue:N0} in {year} = ${adjusted:N0} in {baseYear} labor-gold dollars"
        });
    }

    /// <summary>
    /// Get the LG index data for a specific year.
    /// </summary>
    [HttpGet("Index/{year}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<object> GetIndex(int year)
    {
        var gt = LaborGoldCalculator.GetGt(year);
        var income = MedianIncomeData.GetIncome(year);
        var gold = GoldPriceData.GetPrice(year);

        if (gt is null)
            return NotFound(new { Error = $"No data for year {year}" });

        return Ok(new
        {
            Year = year,
            MedianIncome = income,
            GoldPricePerOz = gold,
            OzBuyable = gt,
            LaborGoldRatio1920 = LaborGoldCalculator.GetLaborGoldRatio(year),
                  Era = year switch
                  {
                      <= 1933 => "Classical Gold Standard",
                      <= 1971 => "Bretton Woods (Fixed $35/oz)",
                  <= 1980 => "Post-Nixon Shock Correction",
                  <= 2000 => "Stabilization Era",
                  <= 2010 => "Gold Bull Market",
                  _ => "Modern Gold Repricing"
                  }
        });
    }

    /// <summary>
    /// Get the full historical LG index table.
    /// </summary>
    [HttpGet("Index")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetFullIndex()
    {
        var years = GoldPriceData.Prices.Keys
        .Union(MedianIncomeData.Incomes.Keys)
        .Distinct()
        .OrderBy(y => y)
        .Select(year => new
        {
            Year = year,
            MedianIncome = MedianIncomeData.GetIncome(year),
                GoldPrice = GoldPriceData.GetPrice(year),
                OzBuyable = LaborGoldCalculator.GetGt(year),
                Ratio = LaborGoldCalculator.GetLaborGoldRatio(year)
        })
        .Where(x => x.OzBuyable.HasValue);

        return Ok(years);
    }

    /// <summary>
    /// Get all movies in the library ranked by LG-adjusted gross.
    /// </summary>
    /// <param name="limit">Maximum number of results (default 50).</param>
    [HttpGet("Rankings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetRankings(int limit = 50)
    {
        var config = Plugin.Instance?.Configuration;
        var prefix = config?.TagPrefix ?? "LG";

        var movies = _libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
        {
            IncludeItemTypes = new[] { Jellyfin.Data.Enums.BaseItemKind.Movie },
            Recursive = true
        })
        .OfType<Movie>()
        .Select(m =>
        {
            var grossTag = m.Tags?.FirstOrDefault(t =>
            t.StartsWith($"{prefix}:Gross:", StringComparison.Ordinal));
            var tierTag = m.Tags?.FirstOrDefault(t =>
            t.StartsWith($"{prefix}:Tier:", StringComparison.Ordinal));
            var roiTag = m.Tags?.FirstOrDefault(t =>
            t.StartsWith($"{prefix}:ROI:", StringComparison.Ordinal));

            return new
            {
                m.Name,
                Year = m.ProductionYear,
                ImdbId = m.GetProviderId(MetadataProvider.Imdb),
                LGAdjustedGross = grossTag?.Split(':').Last(),
                Tier = tierTag?.Split(':').Last(),
                ROI = roiTag?.Split(':').Last(),
                CriticRating = m.CriticRating
            };
        })
        .Where(m => m.LGAdjustedGross is not null)
        .OrderByDescending(m => m.CriticRating ?? 0)
        .Take(limit);

        return Ok(movies);
    }
}