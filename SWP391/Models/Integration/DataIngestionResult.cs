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

        /// <summary>
        /// Per-paper citation update details collected during a refresh run.
        /// Only populated by RefreshExistingOpenAlexWorksAsync; empty for fetch-new-works runs.
        /// </summary>
        public List<UpdatedPaperDetail> UpdatedPapers { get; set; } = new();

        /// <summary>
        /// Paper preview data (title, abstract, citation count) for newly saved papers.
        /// Populated during fetch-new-works runs to return preview info to client.
        /// </summary>
        public List<PaperPreviewDto> PaperPreviews { get; set; } = new();
    }
}
