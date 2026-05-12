using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class SyncJob
{
    public long SyncJobId { get; set; }

    public int SourceId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? Status { get; set; }

    public int? RecordsFetched { get; set; }

    public string? ErrorMessage { get; set; }

    public virtual ApiDataSource Source { get; set; } = null!;
}
