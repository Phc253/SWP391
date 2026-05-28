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

        // ── NEW METHODS BELOW ─────────────────────────────────────────────────────────

        // Helper: converts raw DB year-counts into a complete, zero-filled growth series.
        // Why application-side: DB can aggregate counts per year but cannot normalize across
        // the whole result set (divide by max) — that cross-row operation must happen in code.
        // Why zero-fill: a year with 0 papers is valid data, not missing data; frontend charts
        // need a continuous x-axis, and suppressing zero years would hide "dead" periods.
        private static List<YearGrowthPoint> ComputeGrowthPoints(
            List<TrendChartResponse> rawData, int years)
        {
            int currentYear = DateTime.Now.Year;
            int startYear = currentYear - years + 1;

            // Build a fast lookup; years absent from DB have count 0
            var countByYear = rawData.ToDictionary(r => r.Year, r => r.PaperCount);

            var result = new List<YearGrowthPoint>(years);
            for (int year = startYear; year <= currentYear; year++)
            {
                int count = countByYear.TryGetValue(year, out var c) ? c : 0;
                int prevCount = countByYear.TryGetValue(year - 1, out var pc) ? pc : 0;

                double? growthRate = null;
                if (year > startYear)
                {
                    // Guard against divide-by-zero: if baseline is 0 and new count > 0,
                    // treat growth as +100% (a reasonable sentinel for "emerged from nothing").
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

        // Returns a per-year publication chart for a ResearchTopic (analogous to GetKeywordTrendAsync).
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

        // Returns a YoY growth series for a keyword over the requested window.
        // Missing years within the window are zero-filled so the series is always `years` long.
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

        // Returns a YoY growth series for a ResearchTopic over the requested window.
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

        // Returns a ranked list of keywords by composite Research Activity Score (0–100).
        // Score formula: rawScore = RecentPaperCount * 1.0 + GrowthRate * 0.5
        //   • RecentPaperCount reflects current volume (last 3 years)
        //   • GrowthRate reflects momentum (recent vs. baseline year)
        // Normalized to 0–100 so the top keyword always scores 100 and all others are relative.
        // Why normalize in service: the denominator (max raw score) is a cross-row value that
        // cannot be computed in a single EF projection without a self-join.
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

                // Compute raw scores in-memory
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
                double denominator = Math.Max(1.0, maxRaw); // guard: all-zero edge case

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

        // Materializes live paper counts into the PublicationTrends cache table.
        // Intended to be called periodically (e.g., after a data sync) by an Administrator.
        // Returns counts of rows written so the caller can audit the operation.
        public async Task<ServiceResult<ComputeTrendsResponse>> ComputeTrendsAsync()
        {
            try
            {
                int keywordRecords = await _trendRepository.ComputeAndUpsertAllKeywordTrendsAsync();
                int topicRecords = await _trendRepository.ComputeAndUpsertAllTopicTrendsAsync();

                return ServiceResult<ComputeTrendsResponse>.Ok(new ComputeTrendsResponse
                {
                    RecordsWritten = keywordRecords + topicRecords,
                    KeywordRecords = keywordRecords,
                    TopicRecords = topicRecords
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<ComputeTrendsResponse>.Fail(
                    "An error occurred while computing trends: " + ex.Message);
            }
        }
    }
}
