using System.Diagnostics;
using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Trend;
using SWP391.Repositories;
using SWP391.Service.Trends;

namespace SWP391.Service
{
    public class TrendService
    {
        private readonly TrendRepository _trendRepository;

        public TrendService(TrendRepository trendRepository)
        {
            _trendRepository = trendRepository;
        }

        public async Task<ServiceResult<List<TrendChartResponse>>> GetKeywordTrendAsync(string keywordText)
        {
            try
            {
                var data = await _trendRepository.GetTrendByKeywordAsync(keywordText);
                if (!data.Any())
                    return ServiceResult<List<TrendChartResponse>>.Fail("No data found for this keyword. It might not exist or has no papers.");
                return ServiceResult<List<TrendChartResponse>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<TrendChartResponse>>.Fail("An error occurred while fetching keyword trend: " + ex.Message);
            }
        }

        public async Task<ServiceResult<List<TrendingTopicResponse>>> GetTrendingTopicsAsync(int topN = 10)
        {
            try
            {
                var data = await _trendRepository.GetTrendingKeywordsAsync(topN);
                return ServiceResult<List<TrendingTopicResponse>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<TrendingTopicResponse>>.Fail("An error occurred while fetching trending topics: " + ex.Message);
            }
        }

        public async Task<ServiceResult<List<TrendChartResponse>>> GetTopicTrendAsync(int? topicId, string? topicName)
        {
            try
            {
                if (topicId.HasValue)
                {
                    var resolved = await _trendRepository.GetTopicNameByIdAsync(topicId.Value);
                    if (resolved == null)
                        return ServiceResult<List<TrendChartResponse>>.Fail($"Topic with ID {topicId} not found.");
                    topicName = resolved;
                }
                var data = await _trendRepository.GetTrendByTopicAsync(topicName!);
                if (!data.Any())
                    return ServiceResult<List<TrendChartResponse>>.Fail("No data found for this topic. It might not exist or has no papers.");
                return ServiceResult<List<TrendChartResponse>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<TrendChartResponse>>.Fail("An error occurred while fetching topic trend: " + ex.Message);
            }
        }

        public async Task<ServiceResult<KeywordGrowthResponse>> GetKeywordGrowthAsync(string keywordText, int years = 5)
        {
            try
            {
                if (years < 1 || years > 50)
                    return ServiceResult<KeywordGrowthResponse>.Fail("Years must be between 1 and 50.");
                var rawData = await _trendRepository.GetRawYearlyCountByKeywordAsync(keywordText, years);
                return ServiceResult<KeywordGrowthResponse>.Ok(new KeywordGrowthResponse
                {
                    KeywordText = keywordText,
                    GrowthData = ComputeGrowthPoints(rawData, years)
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<KeywordGrowthResponse>.Fail("An error occurred while fetching keyword growth: " + ex.Message);
            }
        }

        public async Task<ServiceResult<TopicGrowthResponse>> GetTopicGrowthAsync(int? topicId, string? topicName, int years = 5)
        {
            try
            {
                if (topicId.HasValue)
                {
                    var resolved = await _trendRepository.GetTopicNameByIdAsync(topicId.Value);
                    if (resolved == null)
                        return ServiceResult<TopicGrowthResponse>.Fail($"Topic with ID {topicId} not found.");
                    topicName = resolved;
                }
                if (years < 1 || years > 50)
                    return ServiceResult<TopicGrowthResponse>.Fail("Years must be between 1 and 50.");
                var rawData = await _trendRepository.GetRawYearlyCountByTopicAsync(topicName!, years);
                return ServiceResult<TopicGrowthResponse>.Ok(new TopicGrowthResponse
                {
                    TopicName = topicName,
                    GrowthData = ComputeGrowthPoints(rawData, years)
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<TopicGrowthResponse>.Fail("An error occurred while fetching topic growth: " + ex.Message);
            }
        }

        private static List<YearGrowthPoint> ComputeGrowthPoints(List<TrendChartResponse> rawData, int years)
        {
            int currentYear = DateTime.Now.Year;
            int startYear = currentYear - years + 1;
            var countByYear = rawData.ToDictionary(r => r.Year, r => r.PaperCount);
            var result = new List<YearGrowthPoint>(years);
            for (int year = startYear; year <= currentYear; year++)
            {
                int count = countByYear.TryGetValue(year, out var c) ? c : 0;
                int prevCount = countByYear.TryGetValue(year - 1, out var pc) ? pc : 0;
                double? growthRate = null;
                if (year > startYear)
                {
                    growthRate = prevCount == 0
                        ? (count > 0 ? 100.0 : 0.0)
                        : Math.Round((double)(count - prevCount) / prevCount * 100, 2);
                }
                result.Add(new YearGrowthPoint { Year = year, PaperCount = count, GrowthRate = growthRate });
            }
            return result;
        }

        // Now uses the canonical enhanced formula via TrendScoring so API and snapshots agree.
        public async Task<ServiceResult<List<ActivityScoreResponse>>> GetActivityScoresAsync(int topN = 10)
        {
            try
            {
                if (topN < 1 || topN > 100)
                    return ServiceResult<List<ActivityScoreResponse>>.Fail("topN must be between 1 and 100.");

                var keywordData = await _trendRepository.GetEnhancedActivityDataAsync();
                var topicData   = await _trendRepository.GetEnhancedTopicActivityDataAsync();
                var all = keywordData.Concat(topicData).ToList();

                if (all.Count == 0)
                    return ServiceResult<List<ActivityScoreResponse>>.Ok(new List<ActivityScoreResponse>());

                var scored = all.Select(item => new { item, Score = TrendScoring.Compute(item) }).ToList();
                double maxRaw = scored.Max(s => s.Score.RawScore);

                var result = scored
                    .Select(s => new ActivityScoreResponse
                    {
                        Name             = s.item.Name,
                        Score            = TrendScoring.Normalize(s.Score.RawScore, maxRaw),
                        RecentPaperCount = s.item.RecentPaperCount,
                        GrowthRate       = s.Score.GrowthRate,
                        Momentum         = s.Score.Momentum,
                        CitationVelocity = s.Score.CitationVelocity,
                        Type             = s.item.KeywordId.HasValue ? "Keyword" : "Topic"
                    })
                    .OrderByDescending(r => r.Score)
                    .Take(topN)
                    .ToList();

                return ServiceResult<List<ActivityScoreResponse>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<ActivityScoreResponse>>.Fail("An error occurred while computing activity scores: " + ex.Message);
            }
        }

        // ── Per-stage entry points (demo-friendly) ──

        public async Task<ServiceResult<ComputeStageResponse>> ComputeKeywordsStageAsync()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                int written = await _trendRepository.ComputeAndUpsertAllKeywordTrendsAsync();
                sw.Stop();
                return ServiceResult<ComputeStageResponse>.Ok(new ComputeStageResponse
                {
                    Stage = "keywords",
                    RecordsWritten = written,
                    DurationMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<ComputeStageResponse>.Fail("Keyword stage failed: " + ex.Message);
            }
        }

        public async Task<ServiceResult<ComputeStageResponse>> ComputeTopicsStageAsync()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                int written = await _trendRepository.ComputeAndUpsertAllTopicTrendsAsync();
                sw.Stop();
                return ServiceResult<ComputeStageResponse>.Ok(new ComputeStageResponse
                {
                    Stage = "topics",
                    RecordsWritten = written,
                    DurationMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<ComputeStageResponse>.Fail("Topic stage failed: " + ex.Message);
            }
        }

        public async Task<ServiceResult<ComputeStageResponse>> ComputeSnapshotsStageAsync()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                int written = await ComputeAndWriteSnapshotsInternalAsync();
                sw.Stop();
                return ServiceResult<ComputeStageResponse>.Ok(new ComputeStageResponse
                {
                    Stage = "snapshots",
                    RecordsWritten = written,
                    DurationMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<ComputeStageResponse>.Fail("Snapshot stage failed: " + ex.Message);
            }
        }

        private async Task<int> ComputeAndWriteSnapshotsInternalAsync()
        {
            var kw = await _trendRepository.GetEnhancedActivityDataAsync();
            var tp = await _trendRepository.GetEnhancedTopicActivityDataAsync();
            var all = kw.Concat(tp).ToList();
            if (all.Count == 0) return 0;

            var scored = all.Select(item => new { item, Score = TrendScoring.Compute(item) }).ToList();
            double maxRaw = scored.Max(s => s.Score.RawScore);
            var now = DateTime.UtcNow;

            var snapshots = scored.Select(s => new TrendSnapshot
            {
                KeywordId        = s.item.KeywordId,
                TopicId          = s.item.TopicId,
                SnapshotDate     = now,
                TrendScore       = TrendScoring.Normalize(s.Score.RawScore, maxRaw),
                GrowthRate       = s.Score.GrowthRate,
                Momentum         = s.Score.Momentum,
                CitationVelocity = s.Score.CitationVelocity,
                PaperCount       = s.item.TotalPaperCount,
                RecentPaperCount = s.item.RecentPaperCount
            }).ToList();

            return await _trendRepository.WriteSnapshotsAsync(snapshots);
        }

        // Orchestrator — signature unchanged so DataSyncService.cs:79 keeps working.
        public async Task<ServiceResult<ComputeTrendsResponse>> ComputeTrendsAsync()
        {
            try
            {
                var stages = new List<ComputeStageResponse>();

                var kwResult = await ComputeKeywordsStageAsync();
                if (!kwResult.Success) return ServiceResult<ComputeTrendsResponse>.Fail(kwResult.Error ?? "Keyword stage failed");
                stages.Add(kwResult.Data!);

                var tpResult = await ComputeTopicsStageAsync();
                if (!tpResult.Success) return ServiceResult<ComputeTrendsResponse>.Fail(tpResult.Error ?? "Topic stage failed");
                stages.Add(tpResult.Data!);

                var snapResult = await ComputeSnapshotsStageAsync();
                if (!snapResult.Success) return ServiceResult<ComputeTrendsResponse>.Fail(snapResult.Error ?? "Snapshot stage failed");
                stages.Add(snapResult.Data!);

                return ServiceResult<ComputeTrendsResponse>.Ok(new ComputeTrendsResponse
                {
                    KeywordRecords   = kwResult.Data!.RecordsWritten,
                    TopicRecords     = tpResult.Data!.RecordsWritten,
                    SnapshotsWritten = snapResult.Data!.RecordsWritten,
                    RecordsWritten   = kwResult.Data.RecordsWritten + tpResult.Data.RecordsWritten,
                    Stages           = stages
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<ComputeTrendsResponse>.Fail("An error occurred while computing trends: " + ex.Message);
            }
        }

        public async Task<ServiceResult<List<TrendSnapshotResponse>>> GetSnapshotHistoryAsync(string keywordText, int days = 30)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keywordText))
                    return ServiceResult<List<TrendSnapshotResponse>>.Fail("Keyword is required.");
                if (days < 1 || days > 365)
                    return ServiceResult<List<TrendSnapshotResponse>>.Fail("Days must be between 1 and 365.");
                var data = await _trendRepository.GetSnapshotHistoryByKeywordAsync(keywordText, days);
                return ServiceResult<List<TrendSnapshotResponse>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<TrendSnapshotResponse>>.Fail("An error occurred while fetching snapshot history: " + ex.Message);
            }
        }

        public async Task<ServiceResult<List<TrendSnapshotResponse>>> GetTopicSnapshotHistoryAsync(int? topicId, string? topicName, int days = 30)
        {
            try
            {
                if (topicId.HasValue)
                {
                    var resolved = await _trendRepository.GetTopicNameByIdAsync(topicId.Value);
                    if (resolved == null)
                        return ServiceResult<List<TrendSnapshotResponse>>.Fail($"Topic with ID {topicId} not found.");
                    topicName = resolved;
                }
                if (days < 1 || days > 365)
                    return ServiceResult<List<TrendSnapshotResponse>>.Fail("Days must be between 1 and 365.");
                var data = await _trendRepository.GetSnapshotHistoryByTopicAsync(topicName!, days);
                return ServiceResult<List<TrendSnapshotResponse>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<TrendSnapshotResponse>>.Fail("An error occurred while fetching topic snapshot history: " + ex.Message);
            }
        }
    }
}
