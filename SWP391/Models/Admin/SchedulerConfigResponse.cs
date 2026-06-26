namespace SWP391.Models.Admin
{
    public class SchedulerConfigResponse
    {
        public bool Enabled { get; set; }
        public string Keyword { get; set; } = null!;
        public int MaxResults { get; set; }
        public int IntervalHours { get; set; }
        public bool FetchNewWorksEnabled { get; set; }
        public bool RefreshExistingWorksEnabled { get; set; }
    }
}
