using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class ApiDataSource
{
    public int SourceId { get; set; }

    public string SourceName { get; set; } = null!;

    public string? BaseUrl { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();

    public virtual ICollection<SyncJob> SyncJobs { get; set; } = new List<SyncJob>();
}
