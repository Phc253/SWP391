using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SWP391.Entities;
using SWP391.Models.Integration;

namespace SWP391.Service
{
    public class DataSyncSchedulerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DataSyncSchedulerOptions _options;
        private readonly ILogger<DataSyncSchedulerHostedService> _logger;

        public DataSyncSchedulerHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<DataSyncSchedulerOptions> options,
            ILogger<DataSyncSchedulerHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_options.RunOnStartup)
            {
                await RunSyncAsync(stoppingToken);
            }

            // Use a short polling interval so runtime changes to IntervalHours take effect quickly.
            // Each tick reads effective config from SystemSettings before deciding whether to sync.
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
            var lastRun = DateTime.UtcNow - TimeSpan.FromDays(1); // ensure first tick can run

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var effective = await GetEffectiveOptionsAsync();

                if (!effective.Enabled)
                    continue;

                if (DateTime.UtcNow - lastRun >= effective.GetInterval())
                {
                    lastRun = DateTime.UtcNow;
                    await RunSyncAsync(stoppingToken, effective);
                }
            }
        }

        // Reads runtime config from SystemSettings; falls back to appsettings values when absent.
        private async Task<DataSyncSchedulerOptions> GetEffectiveOptionsAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ScientificTrendDbContext>();

                var keys = new[] { "DataSync:Enabled", "DataSync:Keyword", "DataSync:MaxResults", "DataSync:IntervalHours" };
                var settingsList = await dbContext.SystemSettings
                    .Where(s => keys.Contains(s.SettingKey))
                    .AsNoTracking()
                    .ToListAsync();

                var settings = settingsList.ToDictionary(s => s.SettingKey, s => s.SettingValue);

                bool enabled = _options.Enabled;
                if (settings.TryGetValue("DataSync:Enabled", out var enStr) && bool.TryParse(enStr, out var enVal))
                    enabled = enVal;

                string keyword = _options.Keyword;
                if (settings.TryGetValue("DataSync:Keyword", out var kwStr) && !string.IsNullOrWhiteSpace(kwStr))
                    keyword = kwStr!;

                int maxResults = _options.MaxResults;
                if (settings.TryGetValue("DataSync:MaxResults", out var mrStr) && int.TryParse(mrStr, out var mrVal))
                    maxResults = mrVal;

                int intervalHours = _options.IntervalHours;
                if (settings.TryGetValue("DataSync:IntervalHours", out var ihStr) && int.TryParse(ihStr, out var ihVal))
                    intervalHours = ihVal;

                return new DataSyncSchedulerOptions
                {
                    Enabled       = enabled,
                    Keyword       = keyword,
                    MaxResults    = maxResults,
                    IntervalHours = intervalHours
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read scheduler config from SystemSettings; using appsettings defaults.");
                return _options;
            }
        }

        private async Task RunSyncAsync(CancellationToken stoppingToken, DataSyncSchedulerOptions? effective = null)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            effective ??= _options;

            _logger.LogInformation(
                "Running scheduled OpenAlex sync. Keyword={Keyword}, MaxResults={MaxResults}.",
                effective.GetKeyword(),
                effective.GetMaxResults());

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dataSyncService = scope.ServiceProvider.GetRequiredService<DataSyncService>();

                var result = await dataSyncService.SyncOpenAlexAsync(
                    effective.GetKeyword(),
                    effective.GetMaxResults());

                if (!result.Success)
                {
                    _logger.LogWarning("Scheduled OpenAlex sync failed: {Error}", result.Error);
                    return;
                }

                _logger.LogInformation(
                    "Scheduled OpenAlex sync completed. SyncJobId={SyncJobId}, RecordsFetched={RecordsFetched}, Status={Status}.",
                    result.Data?.SyncJobId,
                    result.Data?.RecordsFetched,
                    result.Data?.Status);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled OpenAlex sync crashed.");
            }
        }
    }
}
