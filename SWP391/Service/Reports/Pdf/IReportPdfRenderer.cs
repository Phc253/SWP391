using SWP391.Models.Report;

namespace SWP391.Service.Reports.Pdf
{
    public record ReportFilters(int? Year, string? KeywordText);

    public interface IReportPdfRenderer
    {
        byte[] RenderPapersReport(PaperReportResponse data, ReportFilters filters);
        byte[] RenderKeywordStatsReport(IReadOnlyList<KeywordStatReport> data);
    }
}
