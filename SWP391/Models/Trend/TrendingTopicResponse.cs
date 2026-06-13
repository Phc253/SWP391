namespace SWP391.Models.Trend
{
    public class TrendingTopicResponse
    {
        public string Name { get; set; } = null!;
        public int RecentPaperCount { get; set; }
        public string Type { get; set; } = null!; // "Keyword" or "Topic"
    }
}
