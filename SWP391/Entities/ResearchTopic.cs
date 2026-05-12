using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class ResearchTopic
{
    public int TopicId { get; set; }

    public string TopicName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Keyword> Keywords { get; set; } = new List<Keyword>();

    public virtual ICollection<PublicationTrend> PublicationTrends { get; set; } = new List<PublicationTrend>();
}
