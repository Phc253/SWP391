namespace SWP391.Models.Report
{
    public class PaperReportResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<PaperReportItem> Items { get; set; } = new();
    }

    public class PaperReportItem
    {
        public long PaperId { get; set; }
        public string Title { get; set; } = null!;
        public int? PublicationYear { get; set; }
        public int? CitationCount { get; set; }
        public string? JournalName { get; set; }
        public List<string> Keywords { get; set; } = new();
        public List<string> Authors { get; set; } = new();
    }
}
