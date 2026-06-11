namespace SWP391.Models.Admin
{
    public class SchedulerConfigRequest
    {
        public bool? Enabled { get; set; }
        public string? Keyword { get; set; }
        public int? MaxResults { get; set; }
        public int? IntervalHours { get; set; }
    }
}
