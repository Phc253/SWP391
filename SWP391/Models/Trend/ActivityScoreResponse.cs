namespace SWP391.Models.Trend
{
    public class ActivityScoreResponse
    {
        public string Name { get; set; } = null!;
        public double Score { get; set; }           // 0–100 normalized composite score
        public int RecentPaperCount { get; set; }
        public double GrowthRate { get; set; }
        public double Momentum { get; set; }
        public double CitationVelocity { get; set; }
        public string Type { get; set; } = null!;   // "Keyword" or "Topic"
    }
}
