namespace SWP391.Models.Integration
{
    public class DataIngestionResult
    {
        public int FetchedCount { get; set; }

        public int SavedCount { get; set; }

        public int InsertedCount
        {
            get => SavedCount;
            set => SavedCount = value;
        }

        public int UpdatedCount { get; set; }

        public string? NextCursor { get; set; }

        public List<long> NewPaperIds { get; set; } = new();
    }
}
