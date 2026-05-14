using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class TrendSnapshot
{
    public long SnapshotId { get; set; }

    public int? TopicId { get; set; }

    public int? KeywordId { get; set; }

    public DateTime? SnapshotDate { get; set; }

    public double? TrendScore { get; set; }

    public double? GrowthRate { get; set; }

    public virtual Keyword? Keyword { get; set; }

    public virtual ResearchTopic? Topic { get; set; }
}
