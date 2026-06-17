namespace SWP391.Models.Trend
{
    public class ComputeTrendsResponse
    {
        public int RecordsWritten { get; set; }
        public int KeywordRecords { get; set; }
        public int TopicRecords { get; set; }
        public int SnapshotsWritten { get; set; }
        public List<ComputeStageResponse> Stages { get; set; } = new();
    }
}
