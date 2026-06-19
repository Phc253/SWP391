using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class DashboardReport
{
    public long ReportId { get; set; }

    public int? UserId { get; set; }

    public string? ReportName { get; set; }

    public string? ReportType { get; set; }

    public string? FilterConfig { get; set; }

    public DateTime? GeneratedAt { get; set; }

    public virtual User? User { get; set; }
}
