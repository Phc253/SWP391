using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class GroupMember
{
    public int GroupId { get; set; }

    public int UserId { get; set; }

    public string? RoleInGroup { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual ResearchGroup Group { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
