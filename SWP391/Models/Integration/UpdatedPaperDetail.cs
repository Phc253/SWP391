namespace SWP391.Models.Integration
{
    /// <summary>
    /// Detail of a single paper whose citation count was updated during a sync operation.
    /// Returned in DataSyncResponse.UpdatedPapers so the admin UI can display a per-paper changelog.
    /// </summary>
    public class UpdatedPaperDetail
    {
        public long PaperId { get; set; }
        public string Title { get; set; } = null!;

        /// <summary>Citation count stored in the database before this sync.</summary>
        public int OldCitationCount { get; set; }

        /// <summary>Citation count fetched from OpenAlex during this sync.</summary>
        public int NewCitationCount { get; set; }

        /// <summary>UTC timestamp when the citation was updated in this sync run.</summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>Publication year of the paper (null if not available).</summary>
        public int? PublicationYear { get; set; }

        /// <summary>
        /// Comma-separated list of author names for this paper.
        /// Empty string if no authors are available.
        /// </summary>
        public string Authors { get; set; } = string.Empty;
    }
}
