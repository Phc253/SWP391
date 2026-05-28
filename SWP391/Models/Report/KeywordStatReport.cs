namespace SWP391.Models.Report
{
    public class KeywordStatReport
    {
        public string KeywordText { get; set; } = null!;
        public int TotalPapers { get; set; }
        public int? FirstYear { get; set; }
        public int? LastYear { get; set; }
    }
}
