using SWP391.Models.Trend;

namespace SWP391.Models.Integration
{
    public class DataSyncResponse
    {
        public long SyncJobId { get; set; }
        public string SourceName { get; set; } = null!;
        public string Keyword { get; set; } = null!;
        public int MaxResults { get; set; }
        public int RecordsFetched { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? ErrorMessage { get; set; }
        public ComputeTrendsResponse? TrendComputation { get; set; }
    }
}
