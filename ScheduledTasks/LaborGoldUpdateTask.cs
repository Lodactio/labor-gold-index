// ScheduledTasks/LaborGoldUpdateTask.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.LaborGoldIndex.ScheduledTasks;

/// <summary>
/// Scheduled task to calculate LG index for all movies in the library.
/// </summary>
public class LaborGoldUpdateTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly ILogger<LaborGoldUpdateTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LaborGoldUpdateTask"/> class.
    /// </summary>
    public LaborGoldUpdateTask(
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        ILogger<LaborGoldUpdateTask> logger)
    {
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Calculate Labor-Gold Index";

    /// <inheritdoc />
    public string Description =>
    "Fetches box office data and calculates the Labor-Gold adjusted " +
    "gross for all movies in the library.";

    /// <inheritdoc />
    public string Category => "Labor-Gold Index";

    /// <inheritdoc />
    public string Key => "LaborGoldIndexUpdate";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Run weekly by default
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerWeekly,
                DayOfWeek = DayOfWeek.Sunday,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            }
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var movies = _libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
        {
            IncludeItemTypes = new[] { Jellyfin.Data.Enums.BaseItemKind.Movie },
            Recursive = true
        })
        .OfType<Movie>()
        .ToList();

        _logger.LogInformation("Starting Labor-Gold Index calculation for {Count} movies", movies.Count);

        for (int i = 0; i < movies.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var movie = movies[i];
            progress.Report((double)i / movies.Count * 100);

            try
            {
                await _providerManager.RefreshSingleItem(
                    movie,
                    new MetadataRefreshOptions(new DirectoryService(_logger))
                    {
                        MetadataRefreshMode = MetadataRefreshMode.Default,
                        ReplaceAllMetadata = false
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update LG index for {Name}", movie.Name);
            }

            // Rate limiting: be kind to external APIs
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Labor-Gold Index calculation complete");
        progress.Report(100);
    }
}