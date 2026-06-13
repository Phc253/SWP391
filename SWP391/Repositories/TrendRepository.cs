using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models.Trend;

namespace SWP391.Repositories
{
    // Internal record used only by TrendRepository → TrendService for activity scoring.
    // Not a public API response model — kept here to avoid polluting Models/Trend/.
    public record ActivityRawData(string Name, int RecentPaperCount, int BaselinePaperCount);

    // Extended record for the enhanced trend score that incorporates citations and momentum.
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
        // Uses client-side evaluation (AsEnumerable) because EF Core cannot translate
        // a record constructor with multiple correlated subqueries into SQL.
        public async Task<List<ActivityRawData>> GetRawActivityDataAsync(int topN)
        {
            int currentYear = DateTime.Now.Year;
            int recentStart  = currentYear - 2;
            int baselineYear = currentYear - 3;

            var keywords = await _dbContext.Keywords
                .Include(k => k.Papers)
                .ToListAsync();

            return keywords
                .Select(k => new ActivityRawData(
                    k.KeywordText,
                    k.Papers.Count(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= recentStart),
                    k.Papers.Count(p => p.PublicationYear.HasValue && p.PublicationYear.Value == baselineYear)
                ))
                .Where(x => x.RecentPaperCount > 0)
                .OrderByDescending(x => x.RecentPaperCount)
                .Take(topN * 3)
                .ToList();
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

        // Returns raw data for keywords needed to compute the enhanced trend score.
        // Uses client-side evaluation (AsEnumerable) because EF Core cannot translate
        // multiple correlated subqueries inside a record constructor into a single SQL query.
        public async Task<List<EnhancedActivityRawData>> GetEnhancedActivityDataAsync()
        {
            int cur = DateTime.Now.Year;
            int recentStart  = cur - 2;
            int baselineYear = cur - 3;
            int priorYear    = cur - 4;

            var keywords = await _dbContext.Keywords
                .Include(k => k.Papers)
                .ToListAsync();

            return keywords
                .Select(k =>
                {
                    var papers = k.Papers.Where(p => p.PublicationYear.HasValue).ToList();
                    int recentCount   = papers.Count(p => p.PublicationYear!.Value >= recentStart);
                    int baselineCount = papers.Count(p => p.PublicationYear!.Value == baselineYear);
                    int priorCount    = papers.Count(p => p.PublicationYear!.Value == priorYear);
                    int recentCits    = papers.Where(p => p.PublicationYear!.Value >= recentStart)
                                              .Sum(p => p.CitationCount ?? 0);
                    return new EnhancedActivityRawData(
                        k.KeywordText, k.KeywordId, null,
                        papers.Count, recentCount, baselineCount, priorCount, recentCits
                    );
                })
                .Where(x => x.RecentPaperCount > 0)
                .ToList();
        }

        // Same as GetEnhancedActivityDataAsync but scoped to ResearchTopics.
        // Deduplicates papers shared across keywords within the same topic.
        public async Task<List<EnhancedActivityRawData>> GetEnhancedTopicActivityDataAsync()
        {
            int cur = DateTime.Now.Year;
            int recentStart  = cur - 2;
            int baselineYear = cur - 3;
            int priorYear    = cur - 4;

            var topics = await _dbContext.ResearchTopics
                .Include(t => t.Keywords)
                    .ThenInclude(k => k.Papers)
                .ToListAsync();

            return topics
                .Select(t =>
                {
                    var papers = t.Keywords
                        .SelectMany(k => k.Papers)
                        .Where(p => p.PublicationYear.HasValue)
                        .GroupBy(p => p.PaperId)
                        .Select(g => g.First())
                        .ToList();

                    int recentCount   = papers.Count(p => p.PublicationYear!.Value >= recentStart);
                    int baselineCount = papers.Count(p => p.PublicationYear!.Value == baselineYear);
                    int priorCount    = papers.Count(p => p.PublicationYear!.Value == priorYear);
                    int recentCits    = papers.Where(p => p.PublicationYear!.Value >= recentStart)
                                              .Sum(p => p.CitationCount ?? 0);
                    return new EnhancedActivityRawData(
                        t.TopicName, null, t.TopicId,
                        papers.Count, recentCount, baselineCount, priorCount, recentCits
                    );
                })
                .Where(x => x.RecentPaperCount > 0)
                .ToList();
        }

        // Bulk-inserts snapshot rows in a single SaveChangesAsync call.
        public async Task WriteSnapshotsAsync(IEnumerable<TrendSnapshot> snapshots)
        {
            _dbContext.TrendSnapshots.AddRange(snapshots);
            await _dbContext.SaveChangesAsync();
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
    }
}
