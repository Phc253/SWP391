namespace SWP391.Models.Dashboard
{
    public class DashboardSummaryResponse
    {
        public int TotalPapers { get; set; }
        public int TotalKeywords { get; set; }
        public int TotalAuthors { get; set; }
        public int TotalJournals { get; set; }
        public int TotalUsers { get; set; }
        public int TotalTopics { get; set; }
        public List<YearPaperCount> PapersByYear { get; set; } = new();
        public List<TopKeywordStat> TopKeywords { get; set; } = new();
        public DateTime? LastSyncTime { get; set; }
    }

    public class YearPaperCount
    {
        public int Year { get; set; }
        public int Count { get; set; }
    }

    public class TopKeywordStat
    {
        public string Keyword { get; set; } = null!;
        public int PaperCount { get; set; }
    }
}
