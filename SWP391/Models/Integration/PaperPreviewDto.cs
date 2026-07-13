namespace SWP391.Models.Integration
{
    /// <summary>
    /// Minimal paper preview DTO containing essential fields: title, abstract, and citation count.
    /// Used to return newly fetched papers to the client immediately after a fetch operation.
    /// </summary>
    public class PaperPreviewDto
    {
        public long PaperId { get; set; }
        public string? Title { get; set; }
        public string? Abstract { get; set; }
        public int? CitationCount { get; set; }
    }
}
