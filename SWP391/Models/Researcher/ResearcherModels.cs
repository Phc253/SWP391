namespace SWP391.Models.Researcher;

public class ResearcherDashboardResponse
{
    public int Years { get; set; }
    public ResearcherOverviewStats Overview { get; set; } = new();
    public List<ResearcherYearMetricPoint> PublicationTrend { get; set; } = new();
    public List<ResearcherYearMetricPoint> CitationTrend { get; set; } = new();
    public List<ResearcherRankedItemResponse> TrendingKeywords { get; set; } = new();
    public List<ResearcherRankedItemResponse> TrendingTopics { get; set; } = new();
    public List<ResearcherJournalSummaryItem> TopJournals { get; set; } = new();
}

public class ResearcherOverviewStats
{
    public int TotalPapers { get; set; }
    public int TotalCitations { get; set; }
    public int TotalKeywords { get; set; }
    public int TotalTopics { get; set; }
    public int TotalJournals { get; set; }
    public int NewPapersInWindow { get; set; }
}

public class ResearcherYearMetricPoint
{
    public int Year { get; set; }
    public int PaperCount { get; set; }
    public int CitationCount { get; set; }
}

public class ResearcherRankedItemResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int RecentPaperCount { get; set; }
    public int TotalPaperCount { get; set; }
    public double GrowthRate { get; set; }
    public double TrendScore { get; set; }
    public double CitationVelocity { get; set; }
    public DateTime? SnapshotDate { get; set; }
}

public class ResearcherJournalSummaryItem
{
    public int JournalId { get; set; }
    public string JournalName { get; set; } = null!;
    public int PaperCount { get; set; }
    public int CitationCount { get; set; }
    public int? FirstYear { get; set; }
    public int? LastYear { get; set; }
}

public class ResearcherComparisonResponse
{
    public string TargetType { get; set; } = null!;
    public int Years { get; set; }
    public List<ResearcherComparisonItem> Items { get; set; } = new();
}

public class ResearcherComparisonItem
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public int TotalPaperCount { get; set; }
    public int TotalCitationCount { get; set; }
    public double GrowthRate { get; set; }
    public List<ResearcherYearMetricPoint> YearlyMetrics { get; set; } = new();
}

public class ResearcherRelatedTopicResponse
{
    public int TopicId { get; set; }
    public string TopicName { get; set; } = null!;
    public int SharedKeywordCount { get; set; }
    public int PaperCount { get; set; }
    public List<string> MatchedKeywords { get; set; } = new();
}

public class ResearcherWatchlistRequest
{
    public long TargetId { get; set; }
    public string TargetType { get; set; } = null!;
}

public class ResearcherWatchlistItemResponse
{
    public long FollowId { get; set; }
    public long TargetId { get; set; }
    public string TargetType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public int PaperCount { get; set; }
    public int RecentPaperCount { get; set; }
    public double? LatestTrendScore { get; set; }
    public double? LatestGrowthRate { get; set; }
}

public class ResearcherReportResponse
{
    public string ReportType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public DateTime GeneratedAt { get; set; }
    public object Data { get; set; } = null!;
}
