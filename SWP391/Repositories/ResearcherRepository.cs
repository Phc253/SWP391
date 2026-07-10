using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models.Researcher;

namespace SWP391.Repositories;

public class ResearcherRepository
{
    private readonly ScientificTrendDbContext _dbContext;

    public ResearcherRepository(ScientificTrendDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResearcherDashboardResponse> GetDashboardAsync(int years, int topN)
    {
        var currentYear = DateTime.Now.Year;
        var startYear = currentYear - years + 1;

        var totalPapers = await _dbContext.Papers.CountAsync();
        var totalCitations = await _dbContext.Papers.SumAsync(p => p.CitationCount ?? 0);
        var totalKeywords = await _dbContext.Keywords.CountAsync();
        var totalTopics = await _dbContext.ResearchTopics.CountAsync();
        var totalJournals = await _dbContext.Journals.CountAsync();

        var yearly = await _dbContext.Papers
            .Where(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear)
            .GroupBy(p => p.PublicationYear!.Value)
            .Select(g => new ResearcherYearMetricPoint
            {
                Year = g.Key,
                PaperCount = g.Count(),
                CitationCount = g.Sum(p => p.CitationCount ?? 0)
            })
            .ToListAsync();

        var yearlyByYear = yearly.ToDictionary(y => y.Year);
        var completeYearly = Enumerable.Range(startYear, years)
            .Select(year => yearlyByYear.TryGetValue(year, out var point)
                ? point
                : new ResearcherYearMetricPoint { Year = year })
            .ToList();

        var topJournals = await GetTopJournalsAsync(startYear, topN);

        return new ResearcherDashboardResponse
        {
            Years = years,
            Overview = new ResearcherOverviewStats
            {
                TotalPapers = totalPapers,
                TotalCitations = totalCitations,
                TotalKeywords = totalKeywords,
                TotalTopics = totalTopics,
                TotalJournals = totalJournals,
                NewPapersInWindow = completeYearly.Sum(y => y.PaperCount)
            },
            PublicationTrend = completeYearly
                .Select(y => new ResearcherYearMetricPoint { Year = y.Year, PaperCount = y.PaperCount })
                .ToList(),
            CitationTrend = completeYearly
                .Select(y => new ResearcherYearMetricPoint { Year = y.Year, CitationCount = y.CitationCount })
                .ToList(),
            TrendingKeywords = await GetTrendingKeywordsAsync(topN),
            TrendingTopics = await GetTrendingTopicsAsync(topN),
            TopJournals = topJournals
        };
    }

    public async Task<List<ResearcherRankedItemResponse>> GetTrendingKeywordsAsync(int topN)
    {
        var latestSnapshots = await _dbContext.TrendSnapshots
            .Where(s => s.KeywordId.HasValue)
            .GroupBy(s => s.KeywordId!.Value)
            .Select(g => g.OrderByDescending(s => s.SnapshotDate).First())
            .ToListAsync();

        if (latestSnapshots.Count == 0)
        {
            return await GetKeywordFallbackRankingAsync(topN);
        }

        var keywordIds = latestSnapshots.Select(s => s.KeywordId!.Value).ToList();
        var keywords = await _dbContext.Keywords
            .Where(k => keywordIds.Contains(k.KeywordId))
            .Select(k => new
            {
                k.KeywordId,
                k.KeywordText,
                TotalPaperCount = k.Papers.Count()
            })
            .ToDictionaryAsync(k => k.KeywordId);

        return latestSnapshots
            .Where(s => keywords.ContainsKey(s.KeywordId!.Value))
            .Select(s =>
            {
                var keyword = keywords[s.KeywordId!.Value];
                return new ResearcherRankedItemResponse
                {
                    Id = keyword.KeywordId,
                    Name = keyword.KeywordText ?? string.Empty,
                    Type = "Keyword",
                    RecentPaperCount = s.RecentPaperCount,
                    TotalPaperCount = keyword.TotalPaperCount,
                    GrowthRate = s.GrowthRate,
                    TrendScore = s.TrendScore,
                    CitationVelocity = s.CitationVelocity,
                    SnapshotDate = s.SnapshotDate
                };
            })
            .OrderByDescending(i => i.TrendScore)
            .ThenByDescending(i => i.RecentPaperCount)
            .Take(topN)
            .ToList();
    }

    public async Task<List<ResearcherRankedItemResponse>> GetTrendingTopicsAsync(int topN)
    {
        var latestSnapshots = await _dbContext.TrendSnapshots
            .Where(s => s.TopicId.HasValue)
            .GroupBy(s => s.TopicId!.Value)
            .Select(g => g.OrderByDescending(s => s.SnapshotDate).First())
            .ToListAsync();

        if (latestSnapshots.Count == 0)
        {
            return await GetTopicFallbackRankingAsync(topN);
        }

        var topicIds = latestSnapshots.Select(s => s.TopicId!.Value).ToList();
        var topics = await _dbContext.ResearchTopics
            .Where(t => topicIds.Contains(t.TopicId))
            .Select(t => new
            {
                t.TopicId,
                t.TopicName,
                PaperIds = t.Keywords.SelectMany(k => k.Papers).Select(p => p.PaperId).Distinct().ToList()
            })
            .ToListAsync();

        var topicPaperCounts = topics.ToDictionary(t => t.TopicId, t => t.PaperIds.Count);
        var topicNames = topics.ToDictionary(t => t.TopicId, t => t.TopicName ?? string.Empty);

        return latestSnapshots
            .Where(s => topicNames.ContainsKey(s.TopicId!.Value))
            .Select(s => new ResearcherRankedItemResponse
            {
                Id = s.TopicId!.Value,
                Name = topicNames[s.TopicId.Value],
                Type = "ResearchTopic",
                RecentPaperCount = s.RecentPaperCount,
                TotalPaperCount = topicPaperCounts.GetValueOrDefault(s.TopicId.Value),
                GrowthRate = s.GrowthRate,
                TrendScore = s.TrendScore,
                CitationVelocity = s.CitationVelocity,
                SnapshotDate = s.SnapshotDate
            })
            .OrderByDescending(i => i.TrendScore)
            .ThenByDescending(i => i.RecentPaperCount)
            .Take(topN)
            .ToList();
    }

    public async Task<List<ResearcherRankedItemResponse>> GetEmergingTopicsAsync(int years, int topN)
    {
        var currentYear = DateTime.Now.Year;
        var startYear = currentYear - years + 1;
        var recentStart = currentYear - Math.Max(1, years / 2) + 1;

        var rows = await _dbContext.ResearchTopics
            .Select(t => new
            {
                t.TopicId,
                t.TopicName,
                Papers = t.Keywords
                    .SelectMany(k => k.Papers)
                    .Where(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear)
                    .Select(p => new { p.PaperId, p.PublicationYear, p.CitationCount })
                    .Distinct()
                    .ToList()
            })
            .ToListAsync();

        return rows
            .Select(r =>
            {
                var recent = r.Papers.Count(p => p.PublicationYear!.Value >= recentStart);
                var baseline = r.Papers.Count - recent;
                return new ResearcherRankedItemResponse
                {
                    Id = r.TopicId,
                    Name = r.TopicName ?? string.Empty,
                    Type = "ResearchTopic",
                    RecentPaperCount = recent,
                    TotalPaperCount = r.Papers.Count,
                    GrowthRate = ComputeGrowthRate(recent, baseline),
                    TrendScore = ComputeEmergingScore(recent, baseline),
                    CitationVelocity = recent == 0
                        ? 0
                        : Math.Round((double)r.Papers
                            .Where(p => p.PublicationYear!.Value >= recentStart)
                            .Sum(p => p.CitationCount ?? 0) / recent, 2)
                };
            })
            .Where(i => i.RecentPaperCount > 0)
            .OrderByDescending(i => i.GrowthRate)
            .ThenByDescending(i => i.RecentPaperCount)
            .Take(topN)
            .ToList();
    }

    public async Task<List<ResearcherRelatedTopicResponse>> GetRelatedTopicsByTopicAsync(int topicId, int topN)
    {
        var seedKeywordIds = await _dbContext.Keywords
            .Where(k => k.TopicId == topicId)
            .Select(k => k.KeywordId)
            .ToListAsync();

        if (seedKeywordIds.Count == 0)
        {
            return new List<ResearcherRelatedTopicResponse>();
        }

        return await GetRelatedTopicsByKeywordIdsAsync(seedKeywordIds, topicId, topN);
    }

    public async Task<List<ResearcherRelatedTopicResponse>> GetRelatedTopicsByKeywordAsync(string keywordText, int topN)
    {
        var keyword = await _dbContext.Keywords
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.KeywordText != null && k.KeywordText == keywordText);

        if (keyword == null)
        {
            return new List<ResearcherRelatedTopicResponse>();
        }

        return await GetRelatedTopicsByKeywordIdsAsync(
            new[] { keyword.KeywordId },
            keyword.TopicId,
            topN);
    }

    public async Task<List<ResearcherComparisonItem>> CompareKeywordsAsync(IEnumerable<int> keywordIds, int years)
    {
        var ids = keywordIds.Distinct().ToList();
        var currentYear = DateTime.Now.Year;
        var startYear = currentYear - years + 1;

        var rows = await _dbContext.Keywords
            .Where(k => ids.Contains(k.KeywordId))
            .Select(k => new
            {
                k.KeywordId,
                k.KeywordText,
                Papers = k.Papers
                    .Where(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear)
                    .Select(p => new PaperMetric(p.PaperId, p.PublicationYear, p.CitationCount))
                    .Distinct()
                    .ToList()
            })
            .ToListAsync();

        return rows
            .Select(r => BuildComparisonItem(r.KeywordId, r.KeywordText ?? string.Empty, r.Papers, startYear, years))
            .ToList();
    }

    public async Task<List<ResearcherComparisonItem>> CompareTopicsAsync(IEnumerable<int> topicIds, int years)
    {
        var ids = topicIds.Distinct().ToList();
        var currentYear = DateTime.Now.Year;
        var startYear = currentYear - years + 1;

        var rows = await _dbContext.ResearchTopics
            .Where(t => ids.Contains(t.TopicId))
            .Select(t => new
            {
                t.TopicId,
                t.TopicName,
                Papers = t.Keywords
                    .SelectMany(k => k.Papers)
                    .Where(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear)
                    .Select(p => new PaperMetric(p.PaperId, p.PublicationYear, p.CitationCount))
                    .Distinct()
                    .ToList()
            })
            .ToListAsync();

        return rows
            .Select(r => BuildComparisonItem(r.TopicId, r.TopicName ?? string.Empty, r.Papers, startYear, years))
            .ToList();
    }

    public async Task<List<ResearcherWatchlistItemResponse>> GetWatchlistAsync(int userId)
    {
        var follows = await _dbContext.Follows
            .Where(f => f.UserId == userId
                     && f.TargetId.HasValue
                     && f.TargetType != null
                     && (f.TargetType == "Keyword"
                         || f.TargetType == "Journal"
                         || f.TargetType == "ResearchTopic"
                         || f.TargetType == "Topic"))
            .OrderByDescending(f => f.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        if (follows.Count == 0)
        {
            return new List<ResearcherWatchlistItemResponse>();
        }

        var keywordIds = follows
            .Where(f => f.TargetType == "Keyword")
            .Select(f => (int)f.TargetId!.Value)
            .Distinct()
            .ToList();
        var journalIds = follows
            .Where(f => f.TargetType == "Journal")
            .Select(f => (int)f.TargetId!.Value)
            .Distinct()
            .ToList();
        var topicIds = follows
            .Where(f => f.TargetType == "ResearchTopic" || f.TargetType == "Topic")
            .Select(f => (int)f.TargetId!.Value)
            .Distinct()
            .ToList();

        var recentCutoff = DateTime.UtcNow.AddDays(-30);

        var keywords = await _dbContext.Keywords
            .Where(k => keywordIds.Contains(k.KeywordId))
            .Select(k => new
            {
                k.KeywordId,
                k.KeywordText,
                PaperCount = k.Papers.Count(),
                RecentPaperCount = k.Papers.Count(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= recentCutoff)
            })
            .ToDictionaryAsync(k => k.KeywordId);

        var journals = await _dbContext.Journals
            .Where(j => journalIds.Contains(j.JournalId))
            .Select(j => new
            {
                j.JournalId,
                j.JournalName,
                PaperCount = j.Papers.Count(),
                RecentPaperCount = j.Papers.Count(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= recentCutoff)
            })
            .ToDictionaryAsync(j => j.JournalId);

        var topics = await _dbContext.ResearchTopics
            .Where(t => topicIds.Contains(t.TopicId))
            .Select(t => new
            {
                t.TopicId,
                t.TopicName,
                PaperIds = t.Keywords.SelectMany(k => k.Papers).Select(p => p.PaperId).Distinct().ToList(),
                RecentPaperIds = t.Keywords
                    .SelectMany(k => k.Papers)
                    .Where(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= recentCutoff)
                    .Select(p => p.PaperId)
                    .Distinct()
                    .ToList()
            })
            .ToListAsync();

        var topicLookup = topics.ToDictionary(t => t.TopicId);
        var latestKeywordSnapshots = await GetLatestKeywordSnapshotsAsync(keywordIds);
        var latestTopicSnapshots = await GetLatestTopicSnapshotsAsync(topicIds);

        var result = new List<ResearcherWatchlistItemResponse>();
        foreach (var follow in follows)
        {
            var targetId = (int)follow.TargetId!.Value;
            var targetType = NormalizeTargetType(follow.TargetType!);
            var item = new ResearcherWatchlistItemResponse
            {
                FollowId = follow.FollowId,
                TargetId = targetId,
                TargetType = targetType,
                CreatedAt = follow.CreatedAt,
                Name = string.Empty
            };

            if (targetType == "Keyword" && keywords.TryGetValue(targetId, out var keyword))
            {
                item.Name = keyword.KeywordText ?? string.Empty;
                item.PaperCount = keyword.PaperCount;
                item.RecentPaperCount = keyword.RecentPaperCount;
                if (latestKeywordSnapshots.TryGetValue(targetId, out var snapshot))
                {
                    item.LatestTrendScore = snapshot.TrendScore;
                    item.LatestGrowthRate = snapshot.GrowthRate;
                }
            }
            else if (targetType == "Journal" && journals.TryGetValue(targetId, out var journal))
            {
                item.Name = journal.JournalName ?? string.Empty;
                item.PaperCount = journal.PaperCount;
                item.RecentPaperCount = journal.RecentPaperCount;
            }
            else if (targetType == "ResearchTopic" && topicLookup.TryGetValue(targetId, out var topic))
            {
                item.Name = topic.TopicName ?? string.Empty;
                item.PaperCount = topic.PaperIds.Count;
                item.RecentPaperCount = topic.RecentPaperIds.Count;
                if (latestTopicSnapshots.TryGetValue(targetId, out var snapshot))
                {
                    item.LatestTrendScore = snapshot.TrendScore;
                    item.LatestGrowthRate = snapshot.GrowthRate;
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                result.Add(item);
            }
        }

        return result;
    }

    public async Task<Follow?> GetFollowAsync(int userId, long targetId, string targetType)
    {
        var normalizedType = NormalizeTargetType(targetType);
        var aliases = normalizedType == "ResearchTopic"
            ? new[] { "ResearchTopic", "Topic" }
            : new[] { normalizedType };

        return await _dbContext.Follows.FirstOrDefaultAsync(f =>
            f.UserId == userId &&
            f.TargetId == targetId &&
            f.TargetType != null &&
            aliases.Contains(f.TargetType));
    }

    public async Task<Follow> AddFollowAsync(int userId, long targetId, string targetType)
    {
        var follow = new Follow
        {
            UserId = userId,
            TargetId = targetId,
            TargetType = NormalizeTargetType(targetType),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Follows.Add(follow);
        await _dbContext.SaveChangesAsync();
        return follow;
    }

    public async Task<bool> RemoveFollowAsync(Follow follow)
    {
        _dbContext.Follows.Remove(follow);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TargetExistsAsync(long targetId, string targetType)
    {
        return NormalizeTargetType(targetType) switch
        {
            "Keyword" => await _dbContext.Keywords.AnyAsync(k => k.KeywordId == targetId),
            "Journal" => await _dbContext.Journals.AnyAsync(j => j.JournalId == targetId),
            "ResearchTopic" => await _dbContext.ResearchTopics.AnyAsync(t => t.TopicId == targetId),
            _ => false
        };
    }

    public async Task<int?> ResolveKeywordIdAsync(string keywordTextOrId)
    {
        if (int.TryParse(keywordTextOrId, out var id))
        {
            return await _dbContext.Keywords.AnyAsync(k => k.KeywordId == id) ? id : null;
        }

        var normalized = keywordTextOrId.Trim();
        return await _dbContext.Keywords
            .Where(k => k.KeywordText != null && k.KeywordText == normalized)
            .Select(k => (int?)k.KeywordId)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> ResolveTopicIdAsync(string topicNameOrId)
    {
        if (int.TryParse(topicNameOrId, out var id))
        {
            return await _dbContext.ResearchTopics.AnyAsync(t => t.TopicId == id) ? id : null;
        }

        var normalized = topicNameOrId.Trim();
        return await _dbContext.ResearchTopics
            .Where(t => t.TopicName != null && t.TopicName == normalized)
            .Select(t => (int?)t.TopicId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ResearcherJournalSummaryItem>> GetJournalSummaryAsync(int? journalId, string? journalName, int years)
    {
        var currentYear = DateTime.Now.Year;
        var startYear = currentYear - years + 1;

        var query = _dbContext.Journals.AsQueryable();
        if (journalId.HasValue)
        {
            query = query.Where(j => j.JournalId == journalId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(journalName))
        {
            var name = journalName.Trim();
            query = query.Where(j => j.JournalName != null && j.JournalName.Contains(name));
        }

        return await query
            .Where(j => j.Papers.Any(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear))
            .Select(j => new ResearcherJournalSummaryItem
            {
                JournalId = j.JournalId,
                JournalName = j.JournalName ?? string.Empty,
                PaperCount = j.Papers.Count(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear),
                CitationCount = j.Papers
                    .Where(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear)
                    .Sum(p => p.CitationCount ?? 0),
                FirstYear = j.Papers
                    .Where(p => p.PublicationYear.HasValue)
                    .Min(p => (int?)p.PublicationYear),
                LastYear = j.Papers
                    .Where(p => p.PublicationYear.HasValue)
                    .Max(p => (int?)p.PublicationYear)
            })
            .OrderByDescending(j => j.PaperCount)
            .ThenBy(j => j.JournalName)
            .ToListAsync();
    }

    private async Task<List<ResearcherRelatedTopicResponse>> GetRelatedTopicsByKeywordIdsAsync(
        IEnumerable<int> keywordIds,
        int? excludedTopicId,
        int topN)
    {
        var seedKeywordIds = keywordIds.Distinct().ToList();
        var seedPaperIds = await _dbContext.Papers
            .Where(p => p.Keywords.Any(k => seedKeywordIds.Contains(k.KeywordId)))
            .Select(p => p.PaperId)
            .ToListAsync();

        if (seedPaperIds.Count == 0)
        {
            return new List<ResearcherRelatedTopicResponse>();
        }

        var rows = await _dbContext.ResearchTopics
            .Where(t => !excludedTopicId.HasValue || t.TopicId != excludedTopicId.Value)
            .Select(t => new
            {
                t.TopicId,
                t.TopicName,
                MatchedKeywords = t.Keywords
                    .Where(k => k.Papers.Any(p => seedPaperIds.Contains(p.PaperId)))
                    .Select(k => k.KeywordText ?? string.Empty)
                    .ToList(),
                PaperCount = t.Keywords
                    .SelectMany(k => k.Papers)
                    .Where(p => seedPaperIds.Contains(p.PaperId))
                    .Select(p => p.PaperId)
                    .Distinct()
                    .Count()
            })
            .ToListAsync();

        return rows
            .Where(r => r.PaperCount > 0)
            .Select(r => new ResearcherRelatedTopicResponse
            {
                TopicId = r.TopicId,
                TopicName = r.TopicName ?? string.Empty,
                SharedKeywordCount = r.MatchedKeywords.Count,
                PaperCount = r.PaperCount,
                MatchedKeywords = r.MatchedKeywords
                    .Where(k => !string.IsNullOrWhiteSpace(k))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(10)
                    .ToList()
            })
            .OrderByDescending(r => r.PaperCount)
            .ThenByDescending(r => r.SharedKeywordCount)
            .Take(topN)
            .ToList();
    }

    private async Task<List<ResearcherJournalSummaryItem>> GetTopJournalsAsync(int startYear, int topN)
    {
        return await _dbContext.Journals
            .Where(j => j.Papers.Any(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear))
            .Select(j => new ResearcherJournalSummaryItem
            {
                JournalId = j.JournalId,
                JournalName = j.JournalName ?? string.Empty,
                PaperCount = j.Papers.Count(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear),
                CitationCount = j.Papers
                    .Where(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= startYear)
                    .Sum(p => p.CitationCount ?? 0),
                FirstYear = j.Papers
                    .Where(p => p.PublicationYear.HasValue)
                    .Min(p => (int?)p.PublicationYear),
                LastYear = j.Papers
                    .Where(p => p.PublicationYear.HasValue)
                    .Max(p => (int?)p.PublicationYear)
            })
            .OrderByDescending(j => j.PaperCount)
            .ThenByDescending(j => j.CitationCount)
            .Take(topN)
            .ToListAsync();
    }

    private async Task<List<ResearcherRankedItemResponse>> GetKeywordFallbackRankingAsync(int topN)
    {
        var currentYear = DateTime.Now.Year;
        var recentStart = currentYear - 2;
        return await _dbContext.Keywords
            .Where(k => k.Papers.Any())
            .Select(k => new ResearcherRankedItemResponse
            {
                Id = k.KeywordId,
                Name = k.KeywordText ?? string.Empty,
                Type = "Keyword",
                TotalPaperCount = k.Papers.Count(),
                RecentPaperCount = k.Papers.Count(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= recentStart),
                CitationVelocity = k.Papers
                    .Where(p => p.PublicationYear.HasValue && p.PublicationYear.Value >= recentStart)
                    .Sum(p => p.CitationCount ?? 0)
            })
            .OrderByDescending(k => k.RecentPaperCount)
            .ThenByDescending(k => k.CitationVelocity)
            .Take(topN)
            .ToListAsync();
    }

    private async Task<List<ResearcherRankedItemResponse>> GetTopicFallbackRankingAsync(int topN)
    {
        var currentYear = DateTime.Now.Year;
        var recentStart = currentYear - 2;
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
            .Select(r => new ResearcherRankedItemResponse
            {
                Id = r.TopicId,
                Name = r.TopicName ?? string.Empty,
                Type = "ResearchTopic",
                TotalPaperCount = r.Papers.Count,
                RecentPaperCount = r.Papers.Count(p => p.PublicationYear!.Value >= recentStart),
                CitationVelocity = r.Papers
                    .Where(p => p.PublicationYear!.Value >= recentStart)
                    .Sum(p => p.CitationCount ?? 0)
            })
            .Where(t => t.RecentPaperCount > 0)
            .OrderByDescending(t => t.RecentPaperCount)
            .ThenByDescending(t => t.CitationVelocity)
            .Take(topN)
            .ToList();
    }

    private async Task<Dictionary<int, TrendSnapshot>> GetLatestKeywordSnapshotsAsync(IEnumerable<int> keywordIds)
    {
        var ids = keywordIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, TrendSnapshot>();
        }

        var snapshots = await _dbContext.TrendSnapshots
            .Where(s => s.KeywordId.HasValue && ids.Contains(s.KeywordId.Value))
            .GroupBy(s => s.KeywordId!.Value)
            .Select(g => g.OrderByDescending(s => s.SnapshotDate).First())
            .ToListAsync();

        return snapshots.ToDictionary(s => s.KeywordId!.Value);
    }

    private async Task<Dictionary<int, TrendSnapshot>> GetLatestTopicSnapshotsAsync(IEnumerable<int> topicIds)
    {
        var ids = topicIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, TrendSnapshot>();
        }

        var snapshots = await _dbContext.TrendSnapshots
            .Where(s => s.TopicId.HasValue && ids.Contains(s.TopicId.Value))
            .GroupBy(s => s.TopicId!.Value)
            .Select(g => g.OrderByDescending(s => s.SnapshotDate).First())
            .ToListAsync();

        return snapshots.ToDictionary(s => s.TopicId!.Value);
    }

    private static ResearcherComparisonItem BuildComparisonItem(
        long id,
        string name,
        IReadOnlyCollection<PaperMetric> papers,
        int startYear,
        int years)
    {
        var paperData = papers
            .Where(p => p.PublicationYear.HasValue)
            .ToList();

        var grouped = paperData
            .GroupBy(p => p.PublicationYear!.Value)
            .ToDictionary(
                g => g.Key,
                g => new ResearcherYearMetricPoint
                {
                    Year = g.Key,
                    PaperCount = g.Count(),
                    CitationCount = g.Sum(p => p.CitationCount ?? 0)
                });

        var yearly = Enumerable.Range(startYear, years)
            .Select(year => grouped.TryGetValue(year, out var point)
                ? point
                : new ResearcherYearMetricPoint { Year = year })
            .ToList();

        var firstHalf = yearly.Take(Math.Max(1, years / 2)).Sum(y => y.PaperCount);
        var secondHalf = yearly.Skip(Math.Max(1, years / 2)).Sum(y => y.PaperCount);

        return new ResearcherComparisonItem
        {
            Id = id,
            Name = name,
            TotalPaperCount = yearly.Sum(y => y.PaperCount),
            TotalCitationCount = yearly.Sum(y => y.CitationCount),
            GrowthRate = ComputeGrowthRate(secondHalf, firstHalf),
            YearlyMetrics = yearly
        };
    }

    private static string NormalizeTargetType(string targetType)
    {
        var normalized = targetType.Trim();
        if (normalized.Equals("Topic", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("ResearchTopic", StringComparison.OrdinalIgnoreCase))
        {
            return "ResearchTopic";
        }

        if (normalized.Equals("Keyword", StringComparison.OrdinalIgnoreCase))
        {
            return "Keyword";
        }

        if (normalized.Equals("Journal", StringComparison.OrdinalIgnoreCase))
        {
            return "Journal";
        }

        return normalized;
    }

    private static double ComputeGrowthRate(int current, int baseline)
    {
        if (baseline == 0)
        {
            return current > 0 ? 100.0 : 0.0;
        }

        return Math.Round((double)(current - baseline) / baseline * 100, 2);
    }

    private static double ComputeEmergingScore(int current, int baseline)
    {
        var growth = ComputeGrowthRate(current, baseline);
        return Math.Round(growth + Math.Log10(current + 1) * 10, 2);
    }

    private sealed record PaperMetric(long PaperId, int? PublicationYear, int? CitationCount);
}
