using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class UserPreference
{
    public long PreferenceId { get; set; }

    public int? UserId { get; set; }

    public string? PreferredField { get; set; }

    public string? PreferredYearRange { get; set; }

    public string? NotificationFrequency { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
