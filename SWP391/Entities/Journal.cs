using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class Journal
{
    public int JournalId { get; set; }

    public string? JournalName { get; set; }

    public string? Issn { get; set; }

    public string? Publisher { get; set; }

    public string? ContactEmail { get; set; }

    public string? Website { get; set; }

    public double? ImpactFactor { get; set; }

    public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();
}
