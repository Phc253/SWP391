using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class Keyword
{
    public int KeywordId { get; set; }

    public string? KeywordText { get; set; }

    public int? TopicId { get; set; }

    public virtual ICollection<PublicationTrend> PublicationTrends { get; set; } = new List<PublicationTrend>();

    public virtual ResearchTopic? Topic { get; set; }

    public virtual ICollection<TrendSnapshot> TrendSnapshots { get; set; } = new List<TrendSnapshot>();

    public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();
}
