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
        private const string RefreshLastPaperIdKey = "DataSync:RefreshLastPaperId";
        private const string FetchNextCursorKey = "DataSync:FetchNextCursor";
        private const string FetchCursorKeywordKey = "DataSync:FetchCursorKeyword";

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

        public async Task<ServiceResult<DataSyncResponse>> SyncOpenAlexAsync(int maxResults)
        {
            return await RefreshExistingOpenAlexAsync(maxResults);
        }

        public async Task<ServiceResult<DataSyncResponse>> FetchOpenAlexAsync(
            string keyword,
            int maxResults,
            bool useFetchCheckpoint = false)
        {
            return await RunOpenAlexPipelineAsync(
                operation: "FetchOpenAlex",
                keyword: keyword,
                maxResults: maxResults,
                fetchNewWorks: true,
                refreshExistingWorks: false,
                useFetchCheckpoint: useFetchCheckpoint);
        }

        public async Task<ServiceResult<DataSyncResponse>> RefreshExistingOpenAlexAsync(int maxResults)
        {
            return await RunOpenAlexPipelineAsync(
                operation: "SyncOpenAlex",
                keyword: "Existing OpenAlex papers",
                maxResults: maxResults,
                fetchNewWorks: false,
                refreshExistingWorks: true,
                useFetchCheckpoint: false);
        }

        private async Task<ServiceResult<DataSyncResponse>> RunOpenAlexPipelineAsync(
            string operation,
            string keyword,
            int maxResults,
            bool fetchNewWorks,
            bool refreshExistingWorks,
            bool useFetchCheckpoint)
        {
            keyword = string.IsNullOrWhiteSpace(keyword) ? "Computer Science" : keyword.Trim();
            maxResults = Math.Clamp(maxResults, 1, 100);

            if (!fetchNewWorks && !refreshExistingWorks)
            {
                return ServiceResult<DataSyncResponse>.Fail(
                    "At least one OpenAlex operation must be enabled: fetch new works or refresh existing works.");
            }

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
                action: operation + "Triggered",
                targetType: "SyncJob",
                targetId: syncJob.SyncJobId,
                details: $"OpenAlex {operation} started: keyword={keyword}, maxResults={maxResults}");

            try
            {
                var fetchResult = new DataIngestionResult();
                if (fetchNewWorks)
                {
                    fetchResult = useFetchCheckpoint
                        ? await FetchOpenAlexWithCheckpointAsync(keyword, maxResults)
                        : await _integrationService.FetchOpenAlexWorksAsync(keyword, maxResults);
                }

                var refreshResult = refreshExistingWorks
                    ? await RefreshExistingOpenAlexPapersAsync(maxResults)
                    : new DataIngestionResult();

                var externalRecordsFetched = fetchResult.FetchedCount + refreshResult.FetchedCount;
                var recordsInserted = fetchResult.SavedCount + refreshResult.SavedCount;
                var recordsUpdated = fetchResult.UpdatedCount + refreshResult.UpdatedCount;
                var recordsChanged = recordsInserted + recordsUpdated;

                syncJob.RecordsFetched = recordsChanged;

                var warnings = new List<string>();
                int notificationsCreated = 0;

                try
                {
                    notificationsCreated = await _notificationTriggerService.TriggerForNewPapersAsync(
                        fetchResult.NewPaperIds);
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
                    action: operation + "Completed",
                    targetType: "SyncJob",
                    targetId: syncJob.SyncJobId,
                    details: $"Status={syncJob.Status}, ExternalRecordsFetched={externalRecordsFetched}, RecordsInserted={recordsInserted}, RecordsUpdated={recordsUpdated}");

                return ServiceResult<DataSyncResponse>.Ok(new DataSyncResponse
                {
                    Operation = operation,
                    SyncJobId = syncJob.SyncJobId,
                    SourceName = source.SourceName ?? OpenAlexSourceName,
                    Keyword = keyword,
                    MaxResults = maxResults,
                    ExternalRecordsFetched = externalRecordsFetched,
                    RecordsInserted = recordsInserted,
                    RecordsUpdated = recordsUpdated,
                    RecordsFetched = recordsChanged,
                    Status = syncJob.Status ?? "Unknown",
                    StartTime = syncJob.StartTime,
                    EndTime = syncJob.EndTime,
                    ErrorMessage = syncJob.ErrorMessage,
                    NotificationsCreated = notificationsCreated,
                    TrendComputation = trendResult.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAlex {Operation} failed for keyword {Keyword}", operation, keyword);

                syncJob.Status = "Failed";
                syncJob.EndTime = DateTime.UtcNow;
                syncJob.ErrorMessage = ex.Message;
                await _dbContext.SaveChangesAsync();

                await _activityLogService.LogAsync(
                    userId: null,
                    action: operation + "Failed",
                    targetType: "SyncJob",
                    targetId: syncJob.SyncJobId,
                    details: ex.Message);

                return ServiceResult<DataSyncResponse>.Fail($"OpenAlex {operation} failed. SyncJobId={syncJob.SyncJobId}. Error: {ex.Message}");
            }
        }

        private async Task<DataIngestionResult> FetchOpenAlexWithCheckpointAsync(string keyword, int maxResults)
        {
            var storedKeyword = await GetStringSettingAsync(FetchCursorKeywordKey);
            var cursor = await GetStringSettingAsync(FetchNextCursorKey);

            if (!string.Equals(storedKeyword, keyword, StringComparison.OrdinalIgnoreCase))
            {
                cursor = "*";
                await UpsertSettingAsync(FetchCursorKeywordKey, keyword);
            }

            if (string.IsNullOrWhiteSpace(cursor))
            {
                cursor = "*";
            }

            var result = await _integrationService.FetchOpenAlexWorksAsync(keyword, maxResults, cursor);
            await UpsertSettingAsync(FetchCursorKeywordKey, keyword);
            await UpsertSettingAsync(
                FetchNextCursorKey,
                string.IsNullOrWhiteSpace(result.NextCursor) ? "*" : result.NextCursor);

            return result;
        }

        private async Task<DataIngestionResult> RefreshExistingOpenAlexPapersAsync(int maxResults)
        {
            var source = await EnsureOpenAlexSourceAsync();
            var lastPaperId = await GetLongSettingAsync(RefreshLastPaperIdKey);

            var query = _dbContext.Papers
                .Where(p => p.SourceId == source.SourceId && p.ExternalId != null);

            var papers = await query
                .Where(p => p.PaperId > lastPaperId)
                .OrderBy(p => p.PaperId)
                .Take(maxResults)
                .AsNoTracking()
                .ToListAsync();

            if (!papers.Any() && lastPaperId > 0)
            {
                papers = await query
                    .OrderBy(p => p.PaperId)
                    .Take(maxResults)
                    .AsNoTracking()
                    .ToListAsync();
            }

            if (!papers.Any())
            {
                return new DataIngestionResult();
            }

            var result = await _integrationService.RefreshExistingOpenAlexWorksAsync(
                papers.Select(p => p.ExternalId!));

            await UpsertSettingAsync(RefreshLastPaperIdKey, papers.Max(p => p.PaperId).ToString());

            return result;
        }

        private async Task<long> GetLongSettingAsync(string key)
        {
            var value = await _dbContext.SystemSettings
                .Where(s => s.SettingKey == key)
                .Select(s => s.SettingValue)
                .FirstOrDefaultAsync();

            return long.TryParse(value, out var parsed) ? parsed : 0;
        }

        private async Task<string?> GetStringSettingAsync(string key)
        {
            return await _dbContext.SystemSettings
                .Where(s => s.SettingKey == key)
                .Select(s => s.SettingValue)
                .FirstOrDefaultAsync();
        }

        private async Task UpsertSettingAsync(string key, string value)
        {
            var setting = await _dbContext.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    SettingKey = key,
                    SettingValue = value
                };
                _dbContext.SystemSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = value;
            }

            await _dbContext.SaveChangesAsync();
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
