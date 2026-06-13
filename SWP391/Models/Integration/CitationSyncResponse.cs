namespace SWP391.Models.Integration
{
    public class CitationSyncResponse
    {
        public long SyncJobId { get; set; }
        public string SourceName { get; set; } = null!;
        public int MaxPapers { get; set; }
        public int TotalPapersScanned { get; set; }
        public int RecordsFetched { get; set; }
        public int RecordsUpdated { get; set; }
        public int RecordsUnchanged { get; set; }
        public int RecordsFailed { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
