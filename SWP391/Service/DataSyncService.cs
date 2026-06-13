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
        private readonly NotificationTriggerService _notificationTriggerService;
        private readonly ActivityLogService _activityLogService;
        private readonly ILogger<DataSyncService> _logger;

        public DataSyncService(
            ScientificTrendDbContext dbContext,
            AcademicDataIntegrationService integrationService,
            NotificationTriggerService notificationTriggerService,
            ActivityLogService activityLogService,
            ILogger<DataSyncService> logger)
        {
            _dbContext = dbContext;
            _integrationService = integrationService;
            _notificationTriggerService = notificationTriggerService;
            _activityLogService = activityLogService;
            _logger = logger;
        }

        public async Task<ServiceResult<DataSyncResponse>> FetchOpenAlexAsync(string keyword, int maxResults)
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
                action: "OpenAlexFetchTriggered",
                targetType: "SyncJob",
                targetId: syncJob.SyncJobId,
                details: $"OpenAlex fetch started: keyword={keyword}, maxResults={maxResults}");

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
                    action: "OpenAlexFetchCompleted",
                    targetType: "SyncJob",
                    targetId: syncJob.SyncJobId,
                    details: $"Status={syncJob.Status}, RecordsFetched={syncJob.RecordsFetched}");

                return ServiceResult<DataSyncResponse>.Ok(new DataSyncResponse
                {
                    SyncJobId = syncJob.SyncJobId,
                    SourceName = source.SourceName ?? OpenAlexSourceName,
                    Keyword = keyword,
                    MaxResults = maxResults,
                    RecordsFetched = ingestionResult.SavedCount,
                    Status = syncJob.Status ?? "Completed",
                    StartTime = syncJob.StartTime,
                    EndTime = syncJob.EndTime,
                    ErrorMessage = syncJob.ErrorMessage,
                    NotificationsCreated = notificationsCreated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAlex fetch failed for keyword {Keyword}", keyword);

                syncJob.Status = "Failed";
                syncJob.EndTime = DateTime.UtcNow;
                syncJob.ErrorMessage = ex.Message;
                await _dbContext.SaveChangesAsync();

                await _activityLogService.LogAsync(
                    userId: null,
                    action: "OpenAlexFetchFailed",
                    targetType: "SyncJob",
                    targetId: syncJob.SyncJobId,
                    details: ex.Message);

                return ServiceResult<DataSyncResponse>.Fail($"OpenAlex fetch failed. SyncJobId={syncJob.SyncJobId}. Error: {ex.Message}");
            }
        }

        public Task<ServiceResult<DataSyncResponse>> SyncOpenAlexAsync(string keyword, int maxResults)
        {
            return FetchOpenAlexAsync(keyword, maxResults);
        }

        public async Task<ServiceResult<CitationSyncResponse>> SynchronizeOpenAlexCitationsAsync(int maxPapers = 200)
        {
            maxPapers = Math.Clamp(maxPapers, 1, 1000);

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
                action: "CitationSyncTriggered",
                targetType: "SyncJob",
                targetId: syncJob.SyncJobId,
                details: $"OpenAlex citation synchronization started: maxPapers={maxPapers}");

            try
            {
                var papers = await _dbContext.Papers
                    .Where(p => p.SourceId == source.SourceId && p.ExternalId != null && p.ExternalId != "")
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(maxPapers)
                    .ToListAsync();

                int fetched = 0;
                int updated = 0;
                int unchanged = 0;
                int failed = 0;

                foreach (var paper in papers)
                {
                    int? citationCount;
                    try
                    {
                        citationCount = await _integrationService.FetchCitationCountFromOpenAlexAsync(paper.ExternalId!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Citation synchronization failed for PaperId={PaperId}, ExternalId={ExternalId}",
                            paper.PaperId,
                            paper.ExternalId);
                        failed++;
                        continue;
                    }

                    if (!citationCount.HasValue)
                    {
                        failed++;
                        continue;
                    }

                    fetched++;

                    if (paper.CitationCount == citationCount.Value)
                    {
                        unchanged++;
                        continue;
                    }

                    paper.CitationCount = citationCount.Value;
                    updated++;
                }

                syncJob.RecordsFetched = fetched;
                syncJob.EndTime = DateTime.UtcNow;

                if (failed > 0)
                {
                    syncJob.Status = "CompletedWithWarnings";
                    syncJob.ErrorMessage = $"{failed} citation records could not be fetched from OpenAlex.";
                }
                else
                {
                    syncJob.Status = "Completed";
                }

                await _dbContext.SaveChangesAsync();

                await _activityLogService.LogAsync(
                    userId: null,
                    action: "CitationSyncCompleted",
                    targetType: "SyncJob",
                    targetId: syncJob.SyncJobId,
                    details: $"Status={syncJob.Status}, Fetched={fetched}, Updated={updated}, Failed={failed}");

                return ServiceResult<CitationSyncResponse>.Ok(new CitationSyncResponse
                {
                    SyncJobId = syncJob.SyncJobId,
                    SourceName = source.SourceName ?? OpenAlexSourceName,
                    MaxPapers = maxPapers,
                    TotalPapersScanned = papers.Count,
                    RecordsFetched = fetched,
                    RecordsUpdated = updated,
                    RecordsUnchanged = unchanged,
                    RecordsFailed = failed,
                    Status = syncJob.Status ?? "Completed",
                    StartTime = syncJob.StartTime,
                    EndTime = syncJob.EndTime,
                    ErrorMessage = syncJob.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAlex citation synchronization failed.");

                syncJob.Status = "Failed";
                syncJob.EndTime = DateTime.UtcNow;
                syncJob.ErrorMessage = ex.Message;
                await _dbContext.SaveChangesAsync();

                await _activityLogService.LogAsync(
                    userId: null,
                    action: "CitationSyncFailed",
                    targetType: "SyncJob",
                    targetId: syncJob.SyncJobId,
                    details: ex.Message);

                return ServiceResult<CitationSyncResponse>.Fail(
                    $"OpenAlex citation synchronization failed. SyncJobId={syncJob.SyncJobId}. Error: {ex.Message}");
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
