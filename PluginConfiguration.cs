// PluginConfiguration.cs
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.LaborGoldIndex.Configuration;

/// <summary>
/// Configuration for the Labor-Gold Index plugin.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the OMDB API key for fetching box office data.
    /// Free tier: 1,000 requests/day at https://www.omdbapi.com/apikey.aspx
    /// </summary>
    public string OmdbApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the TMDB API key (optional, used as fallback for revenue data).
    /// </summary>
    public string TmdbApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base year for the index (default 1920).
    /// </summary>
    public int BaseYear { get; set; } = 1920;

    /// <summary>
    /// Gets or sets a value indicating whether to store the LG-Adjusted value
    /// in the CriticRating field for sortability.
    /// </summary>
    public bool UseCriticRatingField { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to store LG data in custom tags.
    /// </summary>
    public bool AddLaborGoldTags { get; set; } = true;

    /// <summary>
    /// Gets or sets the custom tag prefix (e.g., "LG:" produces "LG:$2,066M").
    /// </summary>
    public string TagPrefix { get; set; } = "LG";

    /// <summary>
    /// Gets or sets a value indicating whether to append LG data to the movie overview.
    /// </summary>
    public bool AppendToOverview { get; set; } = false;

    /// <summary>
    /// Gets or sets the display currency label.
    /// </summary>
    public string CurrencyLabel { get; set; } = "1920 Gold-$";
}