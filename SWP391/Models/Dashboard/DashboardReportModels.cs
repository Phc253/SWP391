namespace SWP391.Models.Dashboard
{
    public class SaveReportRequest
    {
        public string ReportName { get; set; } = null!;
        public string ReportType { get; set; } = null!;
        public string? FilterConfig { get; set; }
    }

    public class DashboardReportResponse
    {
        public long ReportId { get; set; }
        public int? UserId { get; set; }
        public string? ReportName { get; set; }
        public string? ReportType { get; set; }
        public string? FilterConfig { get; set; }
        public DateTime? GeneratedAt { get; set; }
    }
}
