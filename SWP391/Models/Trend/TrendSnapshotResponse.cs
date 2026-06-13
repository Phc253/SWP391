namespace SWP391.Models.Trend
{
    public class TrendSnapshotResponse
    {
        public long SnapshotId { get; set; }
        public DateTime SnapshotDate { get; set; }
        public double TrendScore { get; set; }
        public double GrowthRate { get; set; }
        public double Momentum { get; set; }
        public double CitationVelocity { get; set; }
        public int PaperCount { get; set; }
        public int RecentPaperCount { get; set; }
    }
}
