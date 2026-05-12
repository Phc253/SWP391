using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class Journal
{
    public int JournalId { get; set; }

    public string JournalName { get; set; } = null!;

    public string? Issn { get; set; }

    public string? Publisher { get; set; }

    public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();
}
