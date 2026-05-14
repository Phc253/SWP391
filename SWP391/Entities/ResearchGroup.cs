using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class ResearchGroup
{
    public int GroupId { get; set; }

    public string GroupName { get; set; } = null!;

    public int OwnerId { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

    public virtual User Owner { get; set; } = null!;
}
