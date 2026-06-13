using SWP391.Models;
using SWP391.Models.Trend;
using SWP391.Repositories;
using SWP391.Entities;

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
                {
                    return ServiceResult<List<TrendChartResponse>>.Fail("No data found for this keyword. It might not exist or has no papers.");
                }

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

        // ── EXISTING METHODS ──────────────────────────────────────────────────────────

        private static List<YearGrowthPoint> ComputeGrowthPoints(
            List<TrendChartResponse> rawData, int years)
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

                result.Add(new YearGrowthPoint
                {
                    Year = year,
                    PaperCount = count,
                    GrowthRate = growthRate
                });
            }

            return result;
        }

        public async Task<ServiceResult<List<TrendChartResponse>>> GetTopicTrendAsync(string topicName)
        {
            try
            {
                var data = await _trendRepository.GetTrendByTopicAsync(topicName);
                if (!data.Any())
                    return ServiceResult<List<TrendChartResponse>>.Fail(
                        "No data found for this topic. It might not exist or has no papers.");

                return ServiceResult<List<TrendChartResponse>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<TrendChartResponse>>.Fail(
                    "An error occurred while fetching topic trend: " + ex.Message);
            }
        }

        public async Task<ServiceResult<KeywordGrowthResponse>> GetKeywordGrowthAsync(
            string keywordText, int years = 5)
        {
            try
            {
                if (years < 1 || years > 50)
                    return ServiceResult<KeywordGrowthResponse>.Fail("Years must be between 1 and 50.");

                var rawData = await _trendRepository.GetRawYearlyCountByKeywordAsync(keywordText, years);
                var growthData = ComputeGrowthPoints(rawData, years);

                return ServiceResult<KeywordGrowthResponse>.Ok(new KeywordGrowthResponse
                {
                    KeywordText = keywordText,
                    GrowthData = growthData
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<KeywordGrowthResponse>.Fail(
                    "An error occurred while fetching keyword growth: " + ex.Message);
            }
        }

        public async Task<ServiceResult<TopicGrowthResponse>> GetTopicGrowthAsync(
            string topicName, int years = 5)
        {
            try
            {
                if (years < 1 || years > 50)
                    return ServiceResult<TopicGrowthResponse>.Fail("Years must be between 1 and 50.");

                var rawData = await _trendRepository.GetRawYearlyCountByTopicAsync(topicName, years);
                var growthData = ComputeGrowthPoints(rawData, years);

                return ServiceResult<TopicGrowthResponse>.Ok(new TopicGrowthResponse
                {
                    TopicName = topicName,
                    GrowthData = growthData
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<TopicGrowthResponse>.Fail(
                    "An error occurred while fetching topic growth: " + ex.Message);
            }
        }

        public async Task<ServiceResult<List<ActivityScoreResponse>>> GetActivityScoresAsync(int topN = 10)
        {
            try
            {
                if (topN < 1 || topN > 100)
                    return ServiceResult<List<ActivityScoreResponse>>.Fail(
                        "topN must be between 1 and 100.");

                var rawList = await _trendRepository.GetRawActivityDataAsync(topN);

                if (!rawList.Any())
                    return ServiceResult<List<ActivityScoreResponse>>.Ok(
                        new List<ActivityScoreResponse>());

                var scored = rawList.Select(item =>
                {
                    double growthRate = item.BaselinePaperCount == 0
                        ? (item.RecentPaperCount > 0 ? 100.0 : 0.0)
                        : Math.Round(
                            (double)(item.RecentPaperCount - item.BaselinePaperCount)
                            / item.BaselinePaperCount * 100, 2);

                    double rawScore = item.RecentPaperCount * 1.0 + growthRate * 0.5;
                    return new { item.Name, item.RecentPaperCount, GrowthRate = growthRate, RawScore = rawScore };
                }).ToList();

                double maxRaw = scored.Max(s => s.RawScore);
                double denominator = Math.Max(1.0, maxRaw);

                var result = scored
                    .Select(s => new ActivityScoreResponse
                    {
                        Name = s.Name,
                        Score = Math.Round(s.RawScore / denominator * 100, 2),
                        RecentPaperCount = s.RecentPaperCount,
                        GrowthRate = s.GrowthRate,
                        Type = "Keyword"
                    })
                    .OrderByDescending(r => r.Score)
                    .Take(topN)
                    .ToList();

                return ServiceResult<List<ActivityScoreResponse>>.Ok(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<ActivityScoreResponse>>.Fail(
                    "An error occurred while computing activity scores: " + ex.Message);
            }
        }

        // ── ENHANCED COMPUTE TRENDS ───────────────────────────────────────────────────

        // Enhanced score formula:
        //   growthRate       = (RecentPaperCount − BaselinePaperCount) / max(1, Baseline) × 100
        //   priorGrowthRate  = (BaselinePaperCount − PriorPaperCount)  / max(1, Prior)    × 100
        //   momentum         = growthRate − priorGrowthRate  (acceleration in percentage points)
        //   citationVelocity = TotalRecentCitations / max(1, RecentPaperCount)
        //   rawScore = Volume×1.0 + Growth×0.5 + Citations×0.3 + Momentum×0.2
        private static (double GrowthRate, double Momentum, double CitationVelocity, double RawScore)
            ComputeEnhancedScore(EnhancedActivityRawData item)
        {
            double growthRate = item.BaselinePaperCount == 0
                ? (item.RecentPaperCount > 0 ? 100.0 : 0.0)
                : Math.Round((double)(item.RecentPaperCount - item.BaselinePaperCount)
                             / item.BaselinePaperCount * 100, 2);

            double priorGrowthRate = item.PriorPaperCount == 0
                ? (item.BaselinePaperCount > 0 ? 100.0 : 0.0)
                : Math.Round((double)(item.BaselinePaperCount - item.PriorPaperCount)
                             / item.PriorPaperCount * 100, 2);

            double momentum = Math.Round(growthRate - priorGrowthRate, 2);

            double citationVelocity = Math.Round(
                (double)item.TotalRecentCitations / Math.Max(1, item.RecentPaperCount), 2);

            double rawScore = item.RecentPaperCount * 1.0
                            + growthRate            * 0.5
                            + citationVelocity      * 0.3
                            + momentum              * 0.2;

            return (growthRate, momentum, citationVelocity, rawScore);
        }

        // Materializes live paper counts into the PublicationTrends cache table and writes
        // a TrendSnapshot row per keyword/topic so score history is preserved across runs.
        public async Task<ServiceResult<ComputeTrendsResponse>> ComputeTrendsAsync()
        {
            try
            {
                int keywordRecords = await _trendRepository.ComputeAndUpsertAllKeywordTrendsAsync();
                int topicRecords   = await _trendRepository.ComputeAndUpsertAllTopicTrendsAsync();

                var now = DateTime.UtcNow;
                var snapshots = new List<TrendSnapshot>();

                var kwData    = await _trendRepository.GetEnhancedActivityDataAsync();
                var topicData = await _trendRepository.GetEnhancedTopicActivityDataAsync();
                var allData   = kwData.Concat(topicData).ToList();

                if (allData.Any())
                {
                    var scored = allData.Select(item =>
                    {
                        var (gr, mom, cv, raw) = ComputeEnhancedScore(item);
                        return new { item, GrowthRate = gr, Momentum = mom, CitVelocity = cv, RawScore = raw };
                    }).ToList();

                    double maxRaw      = scored.Max(s => s.RawScore);
                    double denominator = Math.Max(1.0, maxRaw);

                    snapshots = scored.Select(s => new TrendSnapshot
                    {
                        KeywordId        = s.item.KeywordId,
                        TopicId          = s.item.TopicId,
                        SnapshotDate     = now,
                        TrendScore       = Math.Round(s.RawScore / denominator * 100, 2),
                        GrowthRate       = s.GrowthRate,
                        Momentum         = s.Momentum,
                        CitationVelocity = s.CitVelocity,
                        PaperCount       = s.item.TotalPaperCount,
                        RecentPaperCount = s.item.RecentPaperCount
                    }).ToList();

                    await _trendRepository.WriteSnapshotsAsync(snapshots);
                }

                return ServiceResult<ComputeTrendsResponse>.Ok(new ComputeTrendsResponse
                {
                    RecordsWritten   = keywordRecords + topicRecords,
                    KeywordRecords   = keywordRecords,
                    TopicRecords     = topicRecords,
                    SnapshotsWritten = snapshots.Count
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<ComputeTrendsResponse>.Fail(
                    "An error occurred while computing trends: " + ex.Message);
            }
        }

        // ── SNAPSHOT HISTORY ─────────────────────────────────────────────────────────

        public async Task<ServiceResult<List<TrendSnapshotResponse>>> GetSnapshotHistoryAsync(
            string keywordText, int days = 30)
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
                return ServiceResult<List<TrendSnapshotResponse>>.Fail(
                    "An error occurred while fetching snapshot history: " + ex.Message);
            }
        }

        public async Task<ServiceResult<List<TrendSnapshotResponse>>> GetTopicSnapshotHistoryAsync(
            string topicName, int days = 30)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(topicName))
                    return ServiceResult<List<TrendSnapshotResponse>>.Fail("Topic name is required.");
                if (days < 1 || days > 365)
                    return ServiceResult<List<TrendSnapshotResponse>>.Fail("Days must be between 1 and 365.");

                var data = await _trendRepository.GetSnapshotHistoryByTopicAsync(topicName, days);
                return ServiceResult<List<TrendSnapshotResponse>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<TrendSnapshotResponse>>.Fail(
                    "An error occurred while fetching topic snapshot history: " + ex.Message);
            }
        }
    }
}
