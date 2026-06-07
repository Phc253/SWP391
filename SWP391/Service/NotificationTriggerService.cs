using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class NotificationTriggerService
    {
        private const string PaperRelatedType = "Paper";

        private readonly ScientificTrendDbContext _dbContext;
        private readonly FollowRepository _followRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly ILogger<NotificationTriggerService> _logger;

        public NotificationTriggerService(
            ScientificTrendDbContext dbContext,
            FollowRepository followRepository,
            NotificationRepository notificationRepository,
            ILogger<NotificationTriggerService> logger)
        {
            _dbContext = dbContext;
            _followRepository = followRepository;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<int> TriggerForNewPapersAsync(IEnumerable<long> newPaperIds)
        {
            var paperIds = newPaperIds.Distinct().ToList();
            if (!paperIds.Any())
            {
                return 0;
            }

            var papers = await _dbContext.Papers
                .Where(p => paperIds.Contains(p.PaperId))
                .Include(p => p.Journal)
                .Include(p => p.Keywords)
                    .ThenInclude(k => k.Topic)
                .AsNoTracking()
                .ToListAsync();

            if (!papers.Any())
            {
                return 0;
            }

            var journalIds = papers
                .Where(p => p.JournalId.HasValue)
                .Select(p => (long)p.JournalId!.Value)
                .Distinct()
                .ToList();

            var topicIds = papers
                .SelectMany(p => p.Keywords)
                .Where(k => k.TopicId.HasValue)
                .Select(k => (long)k.TopicId!.Value)
                .Distinct()
                .ToList();

            var journalFollows = journalIds.Any()
                ? await _followRepository.GetFollowsByTargetIdsAsync("Journal", journalIds)
                : new List<Follow>();

            var topicFollows = topicIds.Any()
                ? await _followRepository.GetFollowsByTargetTypesAsync(new[] { "ResearchTopic", "Topic" }, topicIds)
                : new List<Follow>();

            var journalFollowsByTarget = journalFollows
                .GroupBy(f => f.TargetId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var topicFollowsByTarget = topicFollows
                .GroupBy(f => f.TargetId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var candidates = new Dictionary<(int UserId, long PaperId), PaperNotificationCandidate>();

            foreach (var paper in papers)
            {
                if (paper.JournalId.HasValue &&
                    journalFollowsByTarget.TryGetValue(paper.JournalId.Value, out var matchedJournalFollows))
                {
                    var journalName = paper.Journal?.JournalName ?? "unknown journal";
                    foreach (var follow in matchedJournalFollows)
                    {
                        AddMatch(candidates, follow.UserId, paper, $"journal \"{journalName}\"");
                    }
                }

                var paperTopics = paper.Keywords
                    .Where(k => k.TopicId.HasValue)
                    .GroupBy(k => k.TopicId!.Value)
                    .Select(g => new
                    {
                        TopicId = (long)g.Key,
                        TopicName = g.First().Topic?.TopicName ?? g.First().KeywordText
                    });

                foreach (var topic in paperTopics)
                {
                    if (!topicFollowsByTarget.TryGetValue(topic.TopicId, out var matchedTopicFollows))
                    {
                        continue;
                    }

                    foreach (var follow in matchedTopicFollows)
                    {
                        AddMatch(candidates, follow.UserId, paper, $"topic \"{topic.TopicName}\"");
                    }
                }
            }

            if (!candidates.Any())
            {
                return 0;
            }

            var existingNotifications = await _notificationRepository.GetExistingByRelatedAsync(
                PaperRelatedType,
                candidates.Values.Select(c => c.PaperId),
                candidates.Values.Select(c => c.UserId));

            var existingKeys = existingNotifications
                .Where(n => n.RelatedId.HasValue)
                .Select(n => BuildKey(n.UserId, n.RelatedId!.Value))
                .ToHashSet();

            var notifications = candidates.Values
                .Where(c => !existingKeys.Contains(BuildKey(c.UserId, c.PaperId)))
                .Select(c => new Notification
                {
                    UserId = c.UserId,
                    Message = BuildMessage(c),
                    RelatedId = c.PaperId,
                    RelatedType = PaperRelatedType,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            await _notificationRepository.AddRangeAsync(notifications);

            _logger.LogInformation(
                "Created {Count} notifications for {PaperCount} new papers.",
                notifications.Count,
                paperIds.Count);

            return notifications.Count;
        }

        private static void AddMatch(
            Dictionary<(int UserId, long PaperId), PaperNotificationCandidate> candidates,
            int userId,
            Paper paper,
            string reason)
        {
            var key = (userId, paper.PaperId);
            if (!candidates.TryGetValue(key, out var candidate))
            {
                candidate = new PaperNotificationCandidate
                {
                    UserId = userId,
                    PaperId = paper.PaperId,
                    PaperTitle = paper.Title
                };
                candidates[key] = candidate;
            }

            candidate.Reasons.Add(reason);
        }

        private static string BuildMessage(PaperNotificationCandidate candidate)
        {
            var reasons = string.Join(", ", candidate.Reasons.OrderBy(r => r));
            return $"New paper matches your followed {reasons}: {candidate.PaperTitle}";
        }

        private static string BuildKey(int userId, long paperId)
        {
            return $"{userId}:{paperId}";
        }

        private class PaperNotificationCandidate
        {
            public int UserId { get; set; }
            public long PaperId { get; set; }
            public string PaperTitle { get; set; } = null!;
            public HashSet<string> Reasons { get; } = new(StringComparer.OrdinalIgnoreCase);
        }
    }
}
