using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models.Trend;

namespace SWP391.Repositories
{
    public record EnhancedActivityRawData(
        string Name,
        int? KeywordId,
        int? TopicId,
        int TotalPaperCount,
        int RecentPaperCount,
        int BaselinePaperCount,
        int PriorPaperCount,
        int TotalRecentCitations
    );

    public class TrendRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public TrendRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Keyword?> GetKeywordByIdAsync(int keywordId)
        {
            return await _dbContext.Keywords.FindAsync(keywordId);
        }

        public async Task<List<Keyword>> GetKeywordsByIdsAsync(IEnumerable<int> keywordIds)
        {
            return await _dbContext.Keywords.Where(k => keywordIds.Contains(k.KeywordId)).ToListAsync();
        }

        // FR-09: Track publication trends by keyword
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

        // FR-10: View trending research keywords based on recent papers
        public async Task<List<TrendingTopicResponse>> GetTrendingKeywordsAsync(int topN = 10)
        {
            int currentYear = DateTime.Now.Year;
            int startYear = currentYear - 5;

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

        // Publication trend chart for a ResearchTopic, aggregated across all its keywords.
        // Distinct() prevents double-counting papers shared across keywords under the same topic.
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

        // Keyword-level activity data. EF Core 8 translates this to a single SELECT
        // with correlated subqueries against PaperKeywords. No Include() / no client-side eval.
        // Pre-filter applied on the source (k.Papers.Any) instead of on the projected record so EF translates it.
        public async Task<List<EnhancedActivityRawData>> GetEnhancedActivityDataAsync()
        {
            int cur = DateTime.Now.Year;
            int recentStart  = cur - 2;
            int baselineYear = cur - 3;
            int priorYear    = cur - 4;

            var rows = await _dbContext.Keywords
                .Where(k => k.Papers.Any(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= recentStart))
                .Select(k => new
                {
                    k.KeywordText,
                    k.KeywordId,
                    TotalPaperCount    = k.Papers.Count(p => p.PublicationYear.HasValue),
                    RecentPaperCount   = k.Papers.Count(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= recentStart),
                    BaselinePaperCount = k.Papers.Count(p => p.PublicationYear.HasValue && p.PublicationYear.Value == baselineYear),
                    PriorPaperCount    = k.Papers.Count(p => p.PublicationYear.HasValue && p.PublicationYear.Value == priorYear),
                    TotalRecentCitations = k.Papers
                        .Where(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= recentStart)
                        .Sum(p => p.CitationCount ?? 0)
                })
                .ToListAsync();

            return rows.Select(r => new EnhancedActivityRawData(
                r.KeywordText,
                r.KeywordId,
                null,
                r.TotalPaperCount,
                r.RecentPaperCount,
                r.BaselinePaperCount,
                r.PriorPaperCount,
                r.TotalRecentCitations
            )).ToList();
        }

        // Topic-level activity data. Two-stage projection: dedupe papers shared across keywords
        // within a topic, then aggregate. Falls back to ToList() because EF cannot translate
        // outer Counts over an inner Distinct() collection. Volume is small (one row per topic).
        public async Task<List<EnhancedActivityRawData>> GetEnhancedTopicActivityDataAsync()
        {
            int cur = DateTime.Now.Year;
            int recentStart  = cur - 2;
            int baselineYear = cur - 3;
            int priorYear    = cur - 4;

            var rows = await _dbContext.ResearchTopics
                .Select(t => new
                {
                    t.TopicId,
                    t.TopicName,
                    Papers = t.Keywords
                        .SelectMany(k => k.Papers)
                        .Where(p => p.PublicationYear.HasValue)
                        .Select(p => new { p.PaperId, p.PublicationYear, p.CitationCount })
                        .Distinct()
                        .ToList()
                })
                .ToListAsync();

            return rows
                .Select(r => new EnhancedActivityRawData(
                    r.TopicName,
                    (int?)null,
                    r.TopicId,
                    r.Papers.Count,
                    r.Papers.Count(p => p.PublicationYear!.Value >= recentStart),
                    r.Papers.Count(p => p.PublicationYear!.Value == baselineYear),
                    r.Papers.Count(p => p.PublicationYear!.Value == priorYear),
                    r.Papers.Where(p => p.PublicationYear!.Value >= recentStart).Sum(p => p.CitationCount ?? 0)))
                .Where(x => x.RecentPaperCount > 0)
                .ToList();
        }

        // Idempotent upsert backed by UX_PublicationTrends_Keyword_Year filtered unique index.
        // Strategy: aggregate live counts via SQL, partition into existing-vs-new in one round-trip,
        // then UPDATE existing via ExecuteUpdate per row and AddRange new rows in a single SaveChanges.
        public async Task<int> ComputeAndUpsertAllKeywordTrendsAsync()
        {
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

            if (computed.Count == 0) return 0;

            var keywordIds = computed.Select(c => c.KeywordId).Distinct().ToList();
            var existing = await _dbContext.PublicationTrends
                .Where(t => t.KeywordId.HasValue && !t.TopicId.HasValue && keywordIds.Contains(t.KeywordId!.Value))
                .ToListAsync();

            var lookup = existing.ToDictionary(t => (t.KeywordId!.Value, t.TrendYear));
            var now = DateTime.UtcNow;
            int written = 0;

            foreach (var item in computed)
            {
                if (lookup.TryGetValue((item.KeywordId, item.TrendYear), out var row))
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

        // Same shape as keyword variant but topic-scoped with paper dedupe.
        public async Task<int> ComputeAndUpsertAllTopicTrendsAsync()
        {
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

            if (computed.Count == 0) return 0;

            var topicIds = computed.Select(c => c.TopicId).Distinct().ToList();
            var existing = await _dbContext.PublicationTrends
                .Where(t => t.TopicId.HasValue && !t.KeywordId.HasValue && topicIds.Contains(t.TopicId!.Value))
                .ToListAsync();

            var lookup = existing.ToDictionary(t => (t.TopicId!.Value, t.TrendYear));
            var now = DateTime.UtcNow;
            int written = 0;

            foreach (var item in computed)
            {
                if (lookup.TryGetValue((item.TopicId, item.TrendYear), out var row))
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

        public async Task<int> WriteSnapshotsAsync(IEnumerable<TrendSnapshot> snapshots)
        {
            var list = snapshots.ToList();
            if (list.Count == 0) return 0;
            _dbContext.TrendSnapshots.AddRange(list);
            await _dbContext.SaveChangesAsync();
            return list.Count;
        }

        public async Task<List<TrendSnapshotResponse>> GetSnapshotHistoryByKeywordAsync(
            string keywordText, int days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            return await _dbContext.TrendSnapshots
                .Where(s => s.KeywordId.HasValue
                         && s.Keyword!.KeywordText == keywordText
                         && s.SnapshotDate >= cutoff)
                .OrderBy(s => s.SnapshotDate)
                .Select(s => new TrendSnapshotResponse
                {
                    SnapshotId       = s.SnapshotId,
                    SnapshotDate     = s.SnapshotDate,
                    TrendScore       = s.TrendScore,
                    GrowthRate       = s.GrowthRate,
                    Momentum         = s.Momentum,
                    CitationVelocity = s.CitationVelocity,
                    PaperCount       = s.PaperCount,
                    RecentPaperCount = s.RecentPaperCount
                })
                .ToListAsync();
        }

        public async Task<List<TrendSnapshotResponse>> GetSnapshotHistoryByTopicAsync(
            string topicName, int days)
        {
            var cutoff = DateTime.UtcNow.AddDays(-days);
            return await _dbContext.TrendSnapshots
                .Where(s => s.TopicId.HasValue
                         && s.Topic!.TopicName == topicName
                         && s.SnapshotDate >= cutoff)
                .OrderBy(s => s.SnapshotDate)
                .Select(s => new TrendSnapshotResponse
                {
                    SnapshotId       = s.SnapshotId,
                    SnapshotDate     = s.SnapshotDate,
                    TrendScore       = s.TrendScore,
                    GrowthRate       = s.GrowthRate,
                    Momentum         = s.Momentum,
                    CitationVelocity = s.CitationVelocity,
                    PaperCount       = s.PaperCount,
                    RecentPaperCount = s.RecentPaperCount
                })
                .ToListAsync();
        }

        // Latest snapshot per (KeywordId, TopicId) ordered by TrendScore desc.
        // Used by dashboard /summary and /me for snapshot-driven trending lists.
        public async Task<List<TrendSnapshot>> GetLatestTopTrendingAsync(int topN)
        {
            var latestPerKeyword = await _dbContext.TrendSnapshots
                .Where(s => s.KeywordId.HasValue)
                .GroupBy(s => s.KeywordId!.Value)
                .Select(g => g.OrderByDescending(s => s.SnapshotDate).First())
                .ToListAsync();

            var latestPerTopic = await _dbContext.TrendSnapshots
                .Where(s => s.TopicId.HasValue)
                .GroupBy(s => s.TopicId!.Value)
                .Select(g => g.OrderByDescending(s => s.SnapshotDate).First())
                .ToListAsync();

            return latestPerKeyword
                .Concat(latestPerTopic)
                .OrderByDescending(s => s.TrendScore)
                .Take(topN)
                .ToList();
        }
    }
}
