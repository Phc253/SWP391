using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class Paper
{
    public long PaperId { get; set; }

    public string? Title { get; set; }

    public string? Abstract { get; set; }

    public int? PublicationYear { get; set; }

    public int? JournalId { get; set; }

    public int? SourceId { get; set; }

    public string? ExternalId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Journal? Journal { get; set; }

    public virtual ICollection<PaperAuthor> PaperAuthors { get; set; } = new List<PaperAuthor>();

    public virtual ICollection<PaperCitation> PaperCitationCitedPapers { get; set; } = new List<PaperCitation>();

    public virtual ICollection<PaperCitation> PaperCitationCitingPapers { get; set; } = new List<PaperCitation>();

    public virtual ApiDataSource? Source { get; set; }

    public virtual ICollection<Keyword> Keywords { get; set; } = new List<Keyword>();
}
