namespace SWP391.Models.Integration
{
    public class DataIngestionResult
    {
        public int SavedCount { get; set; }
        public List<long> NewPaperIds { get; set; } = new();
    }
}
