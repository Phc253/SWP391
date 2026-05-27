using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models.Trend;

namespace SWP391.Repositories
{
    // Internal record used only by TrendRepository → TrendService for activity scoring.
    // Not a public API response model — kept here to avoid polluting Models/Trend/.
    public record ActivityRawData(string Name, int RecentPaperCount, int BaselinePaperCount);

    public class TrendRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public TrendRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // FR5: Track publication trends by keyword
        public async Task<List<TrendChartResponse>> GetTrendByKeywordAsync(string keywordText)
        {
            return await _dbContext.Keywords
                .Where(k => k.KeywordText == keywordText)
                .SelectMany(k => k.Papers)
                .Where(p => p.PublicationYear.HasValue)
                .GroupBy(p => p.PublicationYear!.Value)
                .Select(g => new TrendChartResponse
                {
                    Year = g.Key,
                    PaperCount = g.Count()
                })
                .OrderBy(t => t.Year)
                .ToListAsync();
        }

        // FR7: View trending research keywords based on recent papers
        public async Task<List<TrendingTopicResponse>> GetTrendingKeywordsAsync(int topN = 10)
        {
            int currentYear = DateTime.Now.Year;
            int startYear = currentYear - 5; // Xét trong 5 năm gần nhất (vì dl mock có thể cũ)

            return await _dbContext.Keywords
                .Select(k => new TrendingTopicResponse
                {
                    Name = k.KeywordText,
                    RecentPaperCount = k.Papers.Count(p => p.PublicationYear >= startYear),
                    Type = "Keyword"
                })
                .Where(k => k.RecentPaperCount > 0)
                .OrderByDescending(k => k.RecentPaperCount)
                .Take(topN)
                .ToListAsync();
        }

        // ── NEW METHODS BELOW ─────────────────────────────────────────────────────────

        // FR-NEW-1: Publication trend chart for a ResearchTopic, aggregated across all its keywords.
        // Distinct() on (PaperId, Year) prevents double-counting papers that belong to multiple
        // keywords under the same topic.
        public async Task<List<TrendChartResponse>> GetTrendByTopicAsync(string topicName)
        {
            return await _dbContext.ResearchTopics
                .Where(t => t.TopicName == topicName)
                .SelectMany(t => t.Keywords)
                .SelectMany(k => k.Papers)
                .Where(p => p.PublicationYear.HasValue)
                .Select(p => new { p.PaperId, p.PublicationYear })
                .Distinct()
                .GroupBy(p => p.PublicationYear!.Value)
                .Select(g => new TrendChartResponse
                {
                    Year = g.Key,
                    PaperCount = g.Count()
                })
                .OrderBy(t => t.Year)
                .ToListAsync();
        }

        // FR-NEW-2: Raw year-by-year counts for a keyword within a rolling window.
        // Returns only years that have at least one paper — the service fills in zeroes for gaps.
        public async Task<List<TrendChartResponse>> GetRawYearlyCountByKeywordAsync(string keywordText, int years)
        {
            int startYear = DateTime.Now.Year - years + 1;

            return await _dbContext.Keywords
                .Where(k => k.KeywordText == keywordText)
                .SelectMany(k => k.Papers)
                .Where(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear)
                .GroupBy(p => p.PublicationYear!.Value)
                .Select(g => new TrendChartResponse
                {
                    Year = g.Key,
                    PaperCount = g.Count()
                })
                .OrderBy(t => t.Year)
                .ToListAsync();
        }

        // FR-NEW-3: Raw year-by-year counts for a ResearchTopic within a rolling window.
        // Distinct() prevents double-counting papers shared across keywords under the same topic.
        public async Task<List<TrendChartResponse>> GetRawYearlyCountByTopicAsync(string topicName, int years)
        {
            int startYear = DateTime.Now.Year - years + 1;

            return await _dbContext.ResearchTopics
                .Where(t => t.TopicName == topicName)
                .SelectMany(t => t.Keywords)
                .SelectMany(k => k.Papers)
                .Where(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear)
                .Select(p => new { p.PaperId, p.PublicationYear })
                .Distinct()
                .GroupBy(p => p.PublicationYear!.Value)
                .Select(g => new TrendChartResponse
                {
                    Year = g.Key,
                    PaperCount = g.Count()
                })
                .OrderBy(t => t.Year)
                .ToListAsync();
        }

        // FR-NEW-4: Raw data needed to compute Research Activity Score in the service layer.
        // RecentPaperCount = last 3 years (for volume component of score).
        // BaselinePaperCount = single year 3 years ago (for growth/momentum component).
        // Over-fetches topN * 3 because score-order differs from raw-count-order after applying
        // the growth weight — ensures the true top-N are not missed before re-ranking.
        public async Task<List<ActivityRawData>> GetRawActivityDataAsync(int topN)
        {
            int currentYear = DateTime.Now.Year;
            int recentStart = currentYear - 2;   // last 3 years inclusive (currentYear-2, -1, 0)
            int baselineYear = currentYear - 3;  // single year used as the growth baseline

            return await _dbContext.Keywords
                .Select(k => new ActivityRawData(
                    k.KeywordText,
                    k.Papers.Count(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= recentStart),
                    k.Papers.Count(p => p.PublicationYear.HasValue && p.PublicationYear.Value == baselineYear)
                ))
                .Where(x => x.RecentPaperCount > 0)
                .OrderByDescending(x => x.RecentPaperCount)
                .Take(topN * 3)
                .ToListAsync();
        }

        // FR-NEW-5: Materialize keyword-level trends into the PublicationTrends table.
        // Uses an in-memory dictionary to upsert because there is no unique DB index on
        // (KeywordId, TrendYear) — a single SaveChangesAsync at the end keeps DB round-trips minimal.
        // NOTE: loads all existing keyword-scoped trend rows into memory — acceptable for current data volume.
        public async Task<int> ComputeAndUpsertAllKeywordTrendsAsync()
        {
            // Step A: Aggregate live paper counts by (KeywordId, Year)
            var computed = await _dbContext.Keywords
                .SelectMany(k => k.Papers, (k, p) => new { k.KeywordId, p.PublicationYear })
                .Where(x => x.PublicationYear.HasValue)
                .GroupBy(x => new { x.KeywordId, x.PublicationYear })
                .Select(g => new
                {
                    g.Key.KeywordId,
                    TrendYear = g.Key.PublicationYear!.Value,
                    PaperCount = g.Count()
                })
                .ToListAsync();

            // Step B: Load all existing keyword-scoped rows (TopicId is null)
            var existing = await _dbContext.PublicationTrends
                .Where(t => t.KeywordId.HasValue && !t.TopicId.HasValue)
                .ToListAsync();

            var lookup = existing.ToDictionary(t => (t.KeywordId!.Value, t.TrendYear));
            var now = DateTime.UtcNow;
            int written = 0;

            foreach (var item in computed)
            {
                var key = (item.KeywordId, item.TrendYear);
                if (lookup.TryGetValue(key, out var row))
                {
                    row.PaperCount = item.PaperCount;
                    row.LastUpdated = now;
                }
                else
                {
                    _dbContext.PublicationTrends.Add(new PublicationTrend
                    {
                        KeywordId = item.KeywordId,
                        TopicId = null,
                        TrendYear = item.TrendYear,
                        PaperCount = item.PaperCount,
                        LastUpdated = now
                    });
                }
                written++;
            }

            await _dbContext.SaveChangesAsync();
            return written;
        }

        // FR-NEW-6: Materialize topic-level trends into the PublicationTrends table.
        // Distinct() on (TopicId, PaperId) before grouping prevents double-counting papers shared
        // across keywords within the same topic.
        // NOTE: loads all existing topic-scoped trend rows into memory — acceptable for current data volume.
        public async Task<int> ComputeAndUpsertAllTopicTrendsAsync()
        {
            // Step A: Aggregate live paper counts by (TopicId, Year) — deduplicated
            var computed = await _dbContext.ResearchTopics
                .SelectMany(t => t.Keywords, (t, k) => new { t.TopicId, k })
                .SelectMany(tk => tk.k.Papers, (tk, p) => new { tk.TopicId, p.PaperId, p.PublicationYear })
                .Where(x => x.PublicationYear.HasValue)
                .Select(x => new { x.TopicId, x.PaperId, x.PublicationYear })
                .Distinct()
                .GroupBy(x => new { x.TopicId, x.PublicationYear })
                .Select(g => new
                {
                    g.Key.TopicId,
                    TrendYear = g.Key.PublicationYear!.Value,
                    PaperCount = g.Count()
                })
                .ToListAsync();

            // Step B: Load all existing topic-scoped rows (KeywordId is null)
            var existing = await _dbContext.PublicationTrends
                .Where(t => t.TopicId.HasValue && !t.KeywordId.HasValue)
                .ToListAsync();

            var lookup = existing.ToDictionary(t => (t.TopicId!.Value, t.TrendYear));
            var now = DateTime.UtcNow;
            int written = 0;

            foreach (var item in computed)
            {
                var key = (item.TopicId, item.TrendYear);
                if (lookup.TryGetValue(key, out var row))
                {
                    row.PaperCount = item.PaperCount;
                    row.LastUpdated = now;
                }
                else
                {
                    _dbContext.PublicationTrends.Add(new PublicationTrend
                    {
                        KeywordId = null,
                        TopicId = item.TopicId,
                        TrendYear = item.TrendYear,
                        PaperCount = item.PaperCount,
                        LastUpdated = now
                    });
                }
                written++;
            }

            await _dbContext.SaveChangesAsync();
            return written;
        }
    }
}
