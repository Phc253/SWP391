namespace SWP391.Models.Papers;

public class PaperSearchRequest
{
    public string? Q { get; set; }

    public string? Title { get; set; }

    public string? Keyword { get; set; }

    public string? Topic { get; set; }

    public string? Author { get; set; }

    public string? Journal { get; set; }

    public int? PublicationYear { get; set; }

    public List<int>? AuthorIds { get; set; }

    public List<int>? KeywordIds { get; set; }

    public List<int>? TopicIds { get; set; }

    public List<int>? JournalIds { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
