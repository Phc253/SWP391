using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class Notification
{
    public long NotificationId { get; set; }

    public int UserId { get; set; }

    public string Message { get; set; } = null!;

    public long? RelatedId { get; set; }

    public string? RelatedType { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
