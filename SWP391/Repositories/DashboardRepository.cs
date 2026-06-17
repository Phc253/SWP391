using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models.Dashboard;

namespace SWP391.Repositories
{
    public class DashboardRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public DashboardRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DashboardSummaryResponse> GetSummaryAsync()
        {
            // DbContext is not thread-safe, so queries are awaited sequentially.
            var totalPapers   = await _dbContext.Papers.CountAsync();
            var totalKeywords = await _dbContext.Keywords.CountAsync();
            var totalAuthors  = await _dbContext.Authors.CountAsync();
            var totalJournals = await _dbContext.Journals.CountAsync();
            var totalUsers    = await _dbContext.Users.CountAsync();
            var totalTopics   = await _dbContext.ResearchTopics.CountAsync();

            // Kept for backwards-compat with existing FE callers that read TopKeywords.
            var topKeywords = await _dbContext.Keywords
                .Select(k => new TopKeywordStat
                {
                    Keyword = k.KeywordText,
                    PaperCount = k.Papers.Count()
                })
                .Where(k => k.PaperCount > 0)
                .OrderByDescending(k => k.PaperCount)
                .Take(5)
                .ToListAsync();

            var papersByYear = await _dbContext.Papers
                .Where(p => p.PublicationYear.HasValue)
                .GroupBy(p => p.PublicationYear!.Value)
                .Select(g => new YearPaperCount { Year = g.Key, Count = g.Count() })
                .OrderBy(y => y.Year)
                .ToListAsync();

            var lastSyncTime = await _dbContext.SyncJobs
                .Where(j => j.EndTime.HasValue)
                .OrderByDescending(j => j.EndTime)
                .Select(j => j.EndTime)
                .FirstOrDefaultAsync();

            var trending = await GetTrendingFromLatestSnapshotsAsync(5, followedTopicIds: null, followedKeywordIds: null);

            return new DashboardSummaryResponse
            {
                TotalPapers      = totalPapers,
                TotalKeywords    = totalKeywords,
                TotalAuthors     = totalAuthors,
                TotalJournals    = totalJournals,
                TotalUsers       = totalUsers,
                TotalTopics      = totalTopics,
                TopKeywords      = topKeywords,
                PapersByYear     = papersByYear,
                LastSyncTime     = lastSyncTime,
                TrendingKeywords = trending
            };
        }

        public async Task<UserDashboardResponse> GetUserSummaryAsync(int userId)
        {
            // Bookmark + follow breakdowns
            var bookmarks = await _dbContext.Bookmarks
                .Where(b => b.UserId == userId)
                .GroupBy(b => b.TargetType ?? "Unknown")
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            var follows = await _dbContext.Follows
                .Where(f => f.UserId == userId)
                .GroupBy(f => f.TargetType ?? "Unknown")
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            // Papers ingested in the last 30 days from followed journals.
            // Using Paper.CreatedAt (ingestion time) instead of PublicationYear because OpenAlex
            // backfills historical works during sync — "new for me" means "new to the system."
            var followedJournalIds = await _dbContext.Follows
                .Where(f => f.UserId == userId && f.TargetType == "Journal" && f.TargetId.HasValue)
                .Select(f => (int)f.TargetId!.Value)
                .ToListAsync();

            var since = DateTime.UtcNow.AddDays(-30);
            var followedJournalPapers = followedJournalIds.Count == 0
                ? new List<FollowedJournalPaper>()
                : await _dbContext.Papers
                    .Where(p => p.JournalId.HasValue
                             && followedJournalIds.Contains(p.JournalId.Value)
                             && p.CreatedAt.HasValue
                             && p.CreatedAt.Value >= since)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(10)
                    .Select(p => new FollowedJournalPaper
                    {
                        PaperId         = p.PaperId,
                        Title           = p.Title,
                        JournalName     = p.Journal!.JournalName,
                        PublicationYear = p.PublicationYear,
                        CreatedAt       = p.CreatedAt
                    })
                    .ToListAsync();

            var papersFromFollowedJournalsCount = followedJournalIds.Count == 0
                ? 0
                : await _dbContext.Papers.CountAsync(p =>
                    p.JournalId.HasValue
                    && followedJournalIds.Contains(p.JournalId.Value)
                    && p.CreatedAt.HasValue
                    && p.CreatedAt.Value >= since);

            // Trending limited to followed topics + their child keywords.
            var followedTopicIds = await _dbContext.Follows
                .Where(f => f.UserId == userId && f.TargetType == "ResearchTopic" && f.TargetId.HasValue)
                .Select(f => (int)f.TargetId!.Value)
                .ToListAsync();

            var followedKeywordIds = followedTopicIds.Count == 0
                ? new List<int>()
                : await _dbContext.Keywords
                    .Where(k => k.TopicId.HasValue && followedTopicIds.Contains(k.TopicId.Value))
                    .Select(k => k.KeywordId)
                    .ToListAsync();

            var trending = await GetTrendingFromLatestSnapshotsAsync(5, followedTopicIds, followedKeywordIds);

            var recentNotifications = await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new UserNotificationItem
                {
                    NotificationId = n.NotificationId,
                    Message        = n.Message,
                    RelatedId      = n.RelatedId,
                    RelatedType    = n.RelatedType,
                    CreatedAt      = n.CreatedAt
                })
                .ToListAsync();

            // Bookmarked papers grouped by PublicationYear (skip null years).
            var bookmarkedPaperIds = await _dbContext.Bookmarks
                .Where(b => b.UserId == userId && b.TargetType == "Paper" && b.TargetId.HasValue)
                .Select(b => b.TargetId!.Value)
                .ToListAsync();

            var myPapersByYear = bookmarkedPaperIds.Count == 0
                ? new List<YearPaperCount>()
                : await _dbContext.Papers
                    .Where(p => bookmarkedPaperIds.Contains(p.PaperId) && p.PublicationYear.HasValue)
                    .GroupBy(p => p.PublicationYear!.Value)
                    .Select(g => new YearPaperCount { Year = g.Key, Count = g.Count() })
                    .OrderBy(y => y.Year)
                    .ToListAsync();

            return new UserDashboardResponse
            {
                TotalBookmarks                  = bookmarks.Sum(x => x.Count),
                TotalFollows                    = follows.Sum(x => x.Count),
                BookmarksByType                 = bookmarks.ToDictionary(x => x.Type, x => x.Count),
                FollowsByType                   = follows.ToDictionary(x => x.Type, x => x.Count),
                PapersFromFollowedJournalsCount = papersFromFollowedJournalsCount,
                PapersFromFollowedJournals      = followedJournalPapers,
                TrendingFromFollowedTopics      = trending,
                RecentNotifications             = recentNotifications,
                MyPapersByYear                  = myPapersByYear
            };
        }

        // Returns latest snapshot per (KeywordId, TopicId) entity, ordered by TrendScore desc.
        // If followedTopicIds / followedKeywordIds are non-null, restricts to those entities.
        private async Task<List<TrendingItem>> GetTrendingFromLatestSnapshotsAsync(
            int topN, List<int>? followedTopicIds, List<int>? followedKeywordIds)
        {
            IQueryable<TrendSnapshot> keywordQuery = _dbContext.TrendSnapshots
                .Where(s => s.KeywordId.HasValue);
            IQueryable<TrendSnapshot> topicQuery = _dbContext.TrendSnapshots
                .Where(s => s.TopicId.HasValue);

            if (followedKeywordIds is not null)
            {
                if (followedKeywordIds.Count == 0)
                    keywordQuery = keywordQuery.Where(_ => false);
                else
                    keywordQuery = keywordQuery.Where(s => followedKeywordIds.Contains(s.KeywordId!.Value));
            }

            if (followedTopicIds is not null)
            {
                if (followedTopicIds.Count == 0)
                    topicQuery = topicQuery.Where(_ => false);
                else
                    topicQuery = topicQuery.Where(s => followedTopicIds.Contains(s.TopicId!.Value));
            }

            var latestPerKeyword = await keywordQuery
                .GroupBy(s => s.KeywordId!.Value)
                .Select(g => g.OrderByDescending(s => s.SnapshotDate).First())
                .ToListAsync();

            var latestPerTopic = await topicQuery
                .GroupBy(s => s.TopicId!.Value)
                .Select(g => g.OrderByDescending(s => s.SnapshotDate).First())
                .ToListAsync();

            var keywordIds = latestPerKeyword.Select(s => s.KeywordId!.Value).ToList();
            var topicIds = latestPerTopic.Select(s => s.TopicId!.Value).ToList();

            var keywordNames = await _dbContext.Keywords
                .Where(k => keywordIds.Contains(k.KeywordId))
                .ToDictionaryAsync(k => k.KeywordId, k => k.KeywordText);
            var topicNames = await _dbContext.ResearchTopics
                .Where(t => topicIds.Contains(t.TopicId))
                .ToDictionaryAsync(t => t.TopicId, t => t.TopicName);

            var items = latestPerKeyword
                .Select(s => new TrendingItem
                {
                    Name             = keywordNames.GetValueOrDefault(s.KeywordId!.Value, ""),
                    Type             = "Keyword",
                    TrendScore       = s.TrendScore,
                    GrowthRate       = s.GrowthRate,
                    RecentPaperCount = s.RecentPaperCount,
                    SnapshotDate     = s.SnapshotDate
                })
                .Concat(latestPerTopic.Select(s => new TrendingItem
                {
                    Name             = topicNames.GetValueOrDefault(s.TopicId!.Value, ""),
                    Type             = "Topic",
                    TrendScore       = s.TrendScore,
                    GrowthRate       = s.GrowthRate,
                    RecentPaperCount = s.RecentPaperCount,
                    SnapshotDate     = s.SnapshotDate
                }))
                .OrderByDescending(t => t.TrendScore)
                .Take(topN)
                .ToList();

            return items;
        }
    }
}
