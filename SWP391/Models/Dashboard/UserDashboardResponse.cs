namespace SWP391.Models.Dashboard
{
    public class UserDashboardResponse
    {
        public int TotalBookmarks { get; set; }
        public int TotalFollows { get; set; }
        public Dictionary<string, int> BookmarksByType { get; set; } = new();
        public Dictionary<string, int> FollowsByType { get; set; } = new();

        // Papers ingested in the last 30 days (Paper.CreatedAt, NOT PublicationYear —
        // OpenAlex backfills historical works during a sync).
        public int PapersFromFollowedJournalsCount { get; set; }
        public List<FollowedJournalPaper> PapersFromFollowedJournals { get; set; } = new();

        public List<TrendingItem> TrendingFromFollowedTopics { get; set; } = new();
        public List<UserNotificationItem> RecentNotifications { get; set; } = new();
        public List<YearPaperCount> MyPapersByYear { get; set; } = new();
    }

    public class FollowedJournalPaper
    {
        public long PaperId { get; set; }
        public string Title { get; set; } = null!;
        public string? JournalName { get; set; }
        public int? PublicationYear { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class UserNotificationItem
    {
        public long NotificationId { get; set; }
        public string? Message { get; set; }
        public long? RelatedId { get; set; }
        public string? RelatedType { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
