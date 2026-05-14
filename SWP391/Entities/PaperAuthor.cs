using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class PaperAuthor
{
    public long PaperId { get; set; }

    public int AuthorId { get; set; }

    public int? AuthorOrder { get; set; }

    public bool? IsCorresponding { get; set; }

    public string? Affiliation { get; set; }

    public virtual Author Author { get; set; } = null!;

    public virtual Paper Paper { get; set; } = null!;
}
