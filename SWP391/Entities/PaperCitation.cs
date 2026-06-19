using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class PaperCitation
{
    public long CitationId { get; set; }

    public long CitingPaperId { get; set; }

    public long CitedPaperId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Paper CitedPaper { get; set; } = null!;

    public virtual Paper CitingPaper { get; set; } = null!;
}
