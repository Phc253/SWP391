using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class ActivityLog
{
    public long LogId { get; set; }

    public int? UserId { get; set; }

    public string? ActionType { get; set; }

    public string? EntityName { get; set; }

    public long? EntityId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
