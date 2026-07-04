namespace SWP391.Models.Papers;

public class PaperListItemResponse
{
    public long PaperId { get; set; }

    public string? Title { get; set; }

    public int? PublicationYear { get; set; }

    public int? CitationCount { get; set; }

    public string? Journal { get; set; }

    public List<string> Authors { get; set; } = new();

    public List<string> Keywords { get; set; } = new();
}
