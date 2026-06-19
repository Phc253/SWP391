using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Repositories;

namespace SWP391.Service
{
    // Runs ComputeTrendsAsync once on startup (if never run before) then every 7 days.
    public class TrendComputeBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TrendComputeBackgroundService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromDays(7);

        public TrendComputeBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<TrendComputeBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run immediately on first startup if no snapshots exist yet.
            await RunIfNeededAsync(stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            try
            {
                while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RunIfNeededAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }

        private async Task RunIfNeededAsync(CancellationToken ct)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ScientificTrendDbContext>();

                // Skip if a snapshot was already written within the last 7 days.
                var cutoff = DateTime.UtcNow.AddDays(-7);
                bool recentSnapshotExists = await db.TrendSnapshots
                    .AnyAsync(s => s.SnapshotDate >= cutoff, ct);

                if (recentSnapshotExists)
                {
                    _logger.LogInformation(
                        "TrendCompute: skipped — a snapshot already exists within the last 7 days.");
                    return;
                }

                bool recentlyComputed = await db.PublicationTrends
                    .AnyAsync(t => t.LastUpdated.HasValue && t.LastUpdated.Value >= cutoff, ct);

                if (recentlyComputed)
                {
                    _logger.LogInformation(
                        "TrendCompute: skipped - publication trends were updated within the last 7 days.");
                    return;
                }

                var trendService = scope.ServiceProvider.GetRequiredService<TrendService>();

                var result = await trendService.ComputeTrendsAsync();
                if (result.Success)
                    _logger.LogInformation(
                        "TrendCompute: wrote {kw} keyword + {tp} topic trend rows and {sn} snapshots.",
                        result.Data!.KeywordRecords, result.Data.TopicRecords, result.Data.SnapshotsWritten);
                else
                    _logger.LogWarning("TrendCompute: compute failed — {msg}", result.Error);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TrendCompute: unhandled exception during weekly run.");
            }
        }
    }
}
