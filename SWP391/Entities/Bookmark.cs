using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class Bookmark
{
    public long BookmarkId { get; set; }

    public int? UserId { get; set; }

    public long? TargetId { get; set; }

    public string? TargetType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
