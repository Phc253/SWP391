namespace SWP391.Models.Admin
{
    public class SyncJobResponse
    {
        public long SyncJobId { get; set; }
        public string SourceName { get; set; } = null!;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Status { get; set; }
        public int? RecordsFetched { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
