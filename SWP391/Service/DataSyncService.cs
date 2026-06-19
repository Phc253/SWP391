using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Integration;

namespace SWP391.Service
{
    public class DataSyncService
    {
        private const string OpenAlexSourceName = "OpenAlex";
        private const string OpenAlexBaseUrl = "https://api.openalex.org";

        private readonly ScientificTrendDbContext _dbContext;
        private readonly AcademicDataIntegrationService _integrationService;
        private readonly TrendService _trendService;
        private readonly NotificationTriggerService _notificationTriggerService;
        private readonly ActivityLogService _activityLogService;
        private readonly ILogger<DataSyncService> _logger;

        public DataSyncService(
            ScientificTrendDbContext dbContext,
            AcademicDataIntegrationService integrationService,
            TrendService trendService,
            NotificationTriggerService notificationTriggerService,
            ActivityLogService activityLogService,
            ILogger<DataSyncService> logger)
        {
            _dbContext = dbContext;
            _integrationService = integrationService;
            _trendService = trendService;
            _notificationTriggerService = notificationTriggerService;
            _activityLogService = activityLogService;
            _logger = logger;
        }

        public async Task<ServiceResult<DataSyncResponse>> SyncOpenAlexAsync(string keyword, int maxResults)
        {
            keyword = string.IsNullOrWhiteSpace(keyword) ? "Computer Science" : keyword.Trim();
            maxResults = Math.Clamp(maxResults, 1, 200);

            var source = await EnsureOpenAlexSourceAsync();
            var syncJob = new SyncJob
            {
                SourceId = source.SourceId,
                StartTime = DateTime.UtcNow,
                Status = "Running",
                RecordsFetched = 0
            };

            _dbContext.SyncJobs.Add(syncJob);
            await _dbContext.SaveChangesAsync();

            await _activityLogService.LogAsync(
                userId: null,
                action: "SyncTriggered",
                targetType: "SyncJob",
                targetId: syncJob.SyncJobId,
                details: $"OpenAlex sync started: keyword={keyword}, maxResults={maxResults}");

            try
            {
                var ingestionResult = await _integrationService.FetchAndSaveDataFromOpenAlexAsync(keyword, maxResults);
                syncJob.RecordsFetched = ingestionResult.SavedCount;

                var warnings = new List<string>();
                int notificationsCreated = 0;

                try
                {
                    notificationsCreated = await _notificationTriggerService.TriggerForNewPapersAsync(
                        ingestionResult.NewPaperIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification trigger failed for SyncJobId={SyncJobId}", syncJob.SyncJobId);
                    warnings.Add("Notification trigger failed: " + ex.Message);
                }

                var trendResult = await _trendService.ComputeTrendsAsync();
                if (!trendResult.Success)
                {
                    warnings.Add("Trend computation failed: " + trendResult.Error);
                }

                if (warnings.Any())
                {
                    syncJob.Status = "CompletedWithWarnings";
                    syncJob.ErrorMessage = string.Join(" | ", warnings);
                }
                else
                {
                    syncJob.Status = "Completed";
                }

                syncJob.EndTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                await _activityLogService.LogAsync(
                    userId: null,
                    action: "SyncCompleted",
                    targetType: "SyncJob",
                    targetId: syncJob.SyncJobId,
                    details: $"Status={syncJob.Status}, RecordsFetched={syncJob.RecordsFetched}");

                return ServiceResult<DataSyncResponse>.Ok(new DataSyncResponse
                {
                    SyncJobId = syncJob.SyncJobId,
                    SourceName = source.SourceName,
                    Keyword = keyword,
                    MaxResults = maxResults,
                    RecordsFetched = ingestionResult.SavedCount,
                    Status = syncJob.Status,
                    StartTime = syncJob.StartTime,
                    EndTime = syncJob.EndTime,
                    ErrorMessage = syncJob.ErrorMessage,
                    NotificationsCreated = notificationsCreated,
                    TrendComputation = trendResult.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAlex sync failed for keyword {Keyword}", keyword);

                syncJob.Status = "Failed";
                syncJob.EndTime = DateTime.UtcNow;
                syncJob.ErrorMessage = ex.Message;
                await _dbContext.SaveChangesAsync();

                await _activityLogService.LogAsync(
                    userId: null,
                    action: "SyncFailed",
                    targetType: "SyncJob",
                    targetId: syncJob.SyncJobId,
                    details: ex.Message);

                return ServiceResult<DataSyncResponse>.Fail($"OpenAlex sync failed. SyncJobId={syncJob.SyncJobId}. Error: {ex.Message}");
            }
        }

        private async Task<ApiDataSource> EnsureOpenAlexSourceAsync()
        {
            var source = await _dbContext.ApiDataSources
                .FirstOrDefaultAsync(s => s.SourceName == OpenAlexSourceName);

            if (source != null)
            {
                if (source.IsActive != true)
                {
                    source.IsActive = true;
                    await _dbContext.SaveChangesAsync();
                }

                return source;
            }

            source = new ApiDataSource
            {
                SourceName = OpenAlexSourceName,
                BaseUrl = OpenAlexBaseUrl,
                IsActive = true
            };

            _dbContext.ApiDataSources.Add(source);
            await _dbContext.SaveChangesAsync();

            return source;
        }
    }
}
