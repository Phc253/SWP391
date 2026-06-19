using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class PublicationTrend
{
    public long TrendId { get; set; }

    public int? TopicId { get; set; }

    public int? KeywordId { get; set; }

    public int? TrendYear { get; set; }

    public int? PaperCount { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual Keyword? Keyword { get; set; }

    public virtual ResearchTopic? Topic { get; set; }
}
