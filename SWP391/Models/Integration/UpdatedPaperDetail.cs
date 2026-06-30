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

        /// <summary>Change in citation count (NewCitationCount - OldCitationCount).</summary>
        public int CitationDelta => NewCitationCount - OldCitationCount;

        /// <summary>Primary research topic name the paper belongs to (may be null if not classified).</summary>
        public string? TopicName { get; set; }

        /// <summary>UTC timestamp when the citation was updated in this sync run.</summary>
        public DateTime UpdatedAt { get; set; }
    }
}
