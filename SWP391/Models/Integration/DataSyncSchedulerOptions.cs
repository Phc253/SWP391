namespace SWP391.Models.Integration
{
    public class DataSyncSchedulerOptions
    {
        public const string SectionName = "DataSyncScheduler";

        public bool Enabled { get; set; } = false;

        public string Keyword { get; set; } = "Computer Science";

        public int MaxResults { get; set; } = 20;

        public int CitationBatchSize { get; set; } = 200;

        public int IntervalHours { get; set; } = 24;

        public bool RunOnStartup { get; set; } = false;

        public TimeSpan GetInterval()
        {
            return TimeSpan.FromHours(Math.Clamp(IntervalHours, 1, 24 * 30));
        }

        public int GetMaxResults()
        {
            return Math.Clamp(MaxResults, 1, 200);
        }

        public int GetCitationBatchSize()
        {
            return Math.Clamp(CitationBatchSize, 1, 1000);
        }

        public string GetKeyword()
        {
            return string.IsNullOrWhiteSpace(Keyword) ? "Computer Science" : Keyword.Trim();
        }
    }
}
