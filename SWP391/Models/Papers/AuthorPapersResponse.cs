namespace SWP391.Models.Papers;

public class AuthorPapersResponse
{
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public int WorkCount { get; set; }
    public int CitationCount { get; set; }
    public PaperListResponse Papers { get; set; } = new();
}
