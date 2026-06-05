using Microsoft.Extensions.Options;
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
            if (!_options.Enabled)
            {
                _logger.LogInformation("Data sync scheduler is disabled.");
                return;
            }

            var interval = _options.GetInterval();
            _logger.LogInformation(
                "Data sync scheduler is enabled. Keyword={Keyword}, MaxResults={MaxResults}, Interval={Interval}.",
                _options.GetKeyword(),
                _options.GetMaxResults(),
                interval);

            if (_options.RunOnStartup)
            {
                await RunSyncAsync(stoppingToken);
            }

            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunSyncAsync(stoppingToken);
            }
        }

        private async Task RunSyncAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dataSyncService = scope.ServiceProvider.GetRequiredService<DataSyncService>();

                var result = await dataSyncService.SyncOpenAlexAsync(
                    _options.GetKeyword(),
                    _options.GetMaxResults());

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
