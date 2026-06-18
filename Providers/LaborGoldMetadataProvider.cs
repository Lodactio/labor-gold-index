// Providers/LaborGoldMetadataProvider.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.LaborGoldIndex.Data;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.LaborGoldIndex.Providers;

/// <summary>
/// Metadata provider that fetches box office data and calculates
/// the Labor-Gold adjusted values for movies.
/// </summary>
public class LaborGoldMetadataProvider : ICustomMetadataProvider<Movie>, IHasOrder
{
    private readonly ILogger<LaborGoldMetadataProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaborGoldMetadataProvider"/> class.
    /// </summary>
    public LaborGoldMetadataProvider(
        ILogger<LaborGoldMetadataProvider> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public string Name => "Labor-Gold Index";

    /// <inheritdoc />
    public int Order => 100; // Run after other metadata providers

    /// <inheritdoc />
    public async Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null) return ItemUpdateType.None;

        var year = item.ProductionYear;
        if (!year.HasValue)
        {
            _logger.LogDebug("Skipping {Name}: no production year", item.Name);
            return ItemUpdateType.None;
        }

        // Try to get box office data
        var (budget, gross) = await FetchBoxOfficeDataAsync(item, cancellationToken)
        .ConfigureAwait(false);

        if (gross <= 0)
        {
            _logger.LogDebug("Skipping {Name}: no box office data found", item.Name);
            return ItemUpdateType.None;
        }

        // Calculate LG-adjusted values
        var result = LaborGoldCalculator.Calculate(
            budget,
            gross,
            year.Value,
            config.BaseYear);

        if (result is null)
        {
            _logger.LogWarning(
                "Could not calculate LG index for {Name} ({Year}): missing historical data",
                               item.Name, year.Value);
            return ItemUpdateType.None;
        }

        _logger.LogInformation(
            "LG Index for {Name} ({Year}): Nominal ${Gross:N0} → LG-Adjusted {Adjusted} " +
            "(Ratio: {Ratio:F3}, Oz buyable: {Oz:F1})",
                               item.Name,
                               year.Value,
                               gross,
                               result.FormattedAdjustedGross,
                               result.LaborGoldRatio,
                               result.GoldOzBuyable);

        var updated = ItemUpdateType.None;

        // Store LG data as tags
        if (config.AddLaborGoldTags)
        {
            var tags = item.Tags?.ToList() ?? new List<string>();

            // Remove old LG tags
            tags.RemoveAll(t => t.StartsWith(config.TagPrefix + ":", StringComparison.Ordinal));

            // Add new LG tags
            tags.Add($"{config.TagPrefix}:Gross:{result.FormattedAdjustedGross}");
            tags.Add($"{config.TagPrefix}:ROI:{result.ReturnOnInvestment:F0}%");
            tags.Add($"{config.TagPrefix}:Ratio:{result.LaborGoldRatio:F3}");
            tags.Add($"{config.TagPrefix}:GoldOz:{result.GoldOzBuyable:F1}");

            // Add tier tag for easy filtering
            var tier = result.AdjustedGross switch
            {
                >= 3_000_000_000m => $"{config.TagPrefix}:Tier:Legendary",
                >= 1_000_000_000m => $"{config.TagPrefix}:Tier:Monumental",
                >= 500_000_000m => $"{config.TagPrefix}:Tier:Epic",
                >= 100_000_000m => $"{config.TagPrefix}:Tier:Blockbuster",
                >= 50_000_000m => $"{config.TagPrefix}:Tier:Hit",
                >= 10_000_000m => $"{config.TagPrefix}:Tier:Success",
                _ => $"{config.TagPrefix}:Tier:Modest"
            };
            tags.Add(tier);

            item.Tags = tags.ToArray();
            updated |= ItemUpdateType.MetadataEdit;
        }

        // Optionally store in CriticRating for sortability
        if (config.UseCriticRatingField)
        {
            // Scale to 0-100 range: log scale of adjusted gross
            // $10M = ~20, $100M = ~40, $1B = ~60, $3B+ = ~80+
            var logValue = (double)result.AdjustedGross;
            if (logValue > 0)
            {
                var scaled = Math.Clamp(
                    (Math.Log10(logValue) - 6) * 20, // 10^6 ($1M) = 0, 10^10 ($10B) = 80
                                        0, 100);
                item.CriticRating = (float)scaled;
                updated |= ItemUpdateType.MetadataEdit;
            }
        }

        // Optionally append to overview
        if (config.AppendToOverview && !string.IsNullOrEmpty(item.Overview))
        {
            var lgSuffix = $"\n\n🪙 Labor-Gold Index ({config.CurrencyLabel}): " +
            $"Gross {result.FormattedAdjustedGross}";

            if (budget > 0)
            {
                lgSuffix += $" | Budget {FormatCurrency(result.AdjustedBudget)}" +
                $" | ROI {result.ReturnOnInvestment:F0}%";
            }

            // Remove old suffix if present
            var overviewLines = item.Overview.Split("\n\n🪙 Labor-Gold Index");
            item.Overview = overviewLines[0] + lgSuffix;
            updated |= ItemUpdateType.MetadataEdit;
        }

        return updated;
    }

    /// <summary>
    /// Fetches box office data from OMDB and/or TMDB.
    /// </summary>
    private async Task<(decimal Budget, decimal Gross)> FetchBoxOfficeDataAsync(
        Movie item,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        decimal budget = 0;
        decimal gross = 0;

        // Try OMDB first (has BoxOffice field for domestic gross)
        if (!string.IsNullOrEmpty(config?.OmdbApiKey))
        {
            var imdbId = item.GetProviderId(MetadataProvider.Imdb);
            if (!string.IsNullOrEmpty(imdbId))
            {
                try
                {
                    var (omdbBudget, omdbGross) = await FetchFromOmdbAsync(
                        imdbId, config.OmdbApiKey, cancellationToken).ConfigureAwait(false);
                        if (omdbGross > 0) gross = omdbGross;
                        if (omdbBudget > 0) budget = omdbBudget;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OMDB fetch failed for {ImdbId}", imdbId);
                }
            }
        }

        // Try TMDB as fallback or supplement (has worldwide revenue + budget)
        if (!string.IsNullOrEmpty(config?.TmdbApiKey))
        {
            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            if (!string.IsNullOrEmpty(tmdbId))
            {
                try
                {
                    var (tmdbBudget, tmdbGross) = await FetchFromTmdbAsync(
                        tmdbId, config.TmdbApiKey, cancellationToken).ConfigureAwait(false);

                        // Prefer TMDB worldwide gross over OMDB domestic
                        if (tmdbGross > gross) gross = tmdbGross;
                        if (tmdbBudget > budget) budget = tmdbBudget;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "TMDB fetch failed for {TmdbId}", tmdbId);
                }
            }
        }

        return (budget, gross);
    }

    private async Task<(decimal Budget, decimal Gross)> FetchFromOmdbAsync(
        string imdbId,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://www.omdbapi.com/?i={imdbId}&apikey={apiKey}";

        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        decimal budget = 0;
        decimal gross = 0;

        // OMDB "BoxOffice" is domestic gross (e.g., "$380,000,000")
        if (root.TryGetProperty("BoxOffice", out var boxOffice))
        {
            var cleaned = boxOffice.GetString()?.Replace("$", "").Replace(",", "");
            if (decimal.TryParse(cleaned, CultureInfo.InvariantCulture, out var val))
                gross = val;
        }

        return (budget, gross);
    }

    private async Task<(decimal Budget, decimal Gross)> FetchFromTmdbAsync(
        string tmdbId,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={apiKey}";

        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        decimal budget = 0;
        decimal gross = 0;

        if (root.TryGetProperty("budget", out var budgetProp))
            budget = budgetProp.GetDecimal();

        if (root.TryGetProperty("revenue", out var revenueProp))
            gross = revenueProp.GetDecimal();

        return (budget, gross);
    }

    private static string FormatCurrency(decimal value)
    {
        if (value >= 1_000_000_000m) return $"${value / 1_000_000_000m:F2}B";
        if (value >= 1_000_000m) return $"${value / 1_000_000m:F1}M";
        if (value >= 1_000m) return $"${value / 1_000m:F0}K";
        return $"${value:F0}";
    }
}