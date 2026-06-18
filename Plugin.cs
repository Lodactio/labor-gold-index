// Plugin.cs
using System;
using System.Collections.Generic;
using Jellyfin.Plugin.LaborGoldIndex.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.LaborGoldIndex;

/// <summary>
/// The Labor-Gold Index plugin for Jellyfin.
/// Calculates inflation-adjusted box office using the Labor-Gold methodology.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer)
    : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "Labor-Gold Index";

    /// <inheritdoc />
    public override string Description =>
    "Calculates Labor-Gold adjusted box office gross for movies. " +
    "Measures real cultural impact by converting box office revenue through " +
    "a median-income-to-gold purchasing power index anchored to 1920.";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
            }
        };
    }
}