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
        private readonly ILogger<DataSyncService> _logger;

        public DataSyncService(
            ScientificTrendDbContext dbContext,
            AcademicDataIntegrationService integrationService,
            TrendService trendService,
            ILogger<DataSyncService> logger)
        {
            _dbContext = dbContext;
            _integrationService = integrationService;
            _trendService = trendService;
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

            try
            {
                int recordsFetched = await _integrationService.FetchAndSaveDataFromOpenAlexAsync(keyword, maxResults);
                syncJob.RecordsFetched = recordsFetched;

                var trendResult = await _trendService.ComputeTrendsAsync();
                if (!trendResult.Success)
                {
                    syncJob.Status = "CompletedWithWarnings";
                    syncJob.ErrorMessage = trendResult.Error;
                }
                else
                {
                    syncJob.Status = "Completed";
                }

                syncJob.EndTime = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                return ServiceResult<DataSyncResponse>.Ok(new DataSyncResponse
                {
                    SyncJobId = syncJob.SyncJobId,
                    SourceName = source.SourceName,
                    Keyword = keyword,
                    MaxResults = maxResults,
                    RecordsFetched = recordsFetched,
                    Status = syncJob.Status,
                    StartTime = syncJob.StartTime,
                    EndTime = syncJob.EndTime,
                    ErrorMessage = syncJob.ErrorMessage,
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
