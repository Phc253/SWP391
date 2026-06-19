namespace SWP391.Models.Trend
{
    public class ComputeStageResponse
    {
        public string Stage { get; set; } = null!;
        public int RecordsWritten { get; set; }
        public long DurationMs { get; set; }
    }
}
