namespace SWP391.Models.Papers;

public class PaperListResponse
{
    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public List<PaperListItemResponse> Items { get; set; } = new();
}
