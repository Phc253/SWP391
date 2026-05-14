using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class Author
{
    public int AuthorId { get; set; }

    public string? AuthorName { get; set; }

    public int? TotalPublications { get; set; }

    public string? ResearchArea { get; set; }

    public virtual ICollection<PaperAuthor> PaperAuthors { get; set; } = new List<PaperAuthor>();
}
