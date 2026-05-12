using System;
using System.Collections.Generic;

namespace SWP391.Entities;

public partial class Author
{
    public int AuthorId { get; set; }

    public string AuthorName { get; set; } = null!;

    public virtual ICollection<Paper> Papers { get; set; } = new List<Paper>();
}
