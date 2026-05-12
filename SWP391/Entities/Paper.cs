using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class Paper
{
    public long PaperId { get; set; }

    public string Title { get; set; } = null!;

    public string? Abstract { get; set; }

    public int? PublicationYear { get; set; }

    public int? JournalId { get; set; }

    public int? SourceId { get; set; }

    public string? ExternalId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Journal? Journal { get; set; }

    public virtual ApiDataSource? Source { get; set; }

    public virtual ICollection<Author> Authors { get; set; } = new List<Author>();

    public virtual ICollection<Keyword> Keywords { get; set; } = new List<Keyword>();
}
