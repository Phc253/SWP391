namespace SWP391.Models.Papers;

public class PaperFacetResponse
{
    public int TotalCount { get; set; }

    public List<PaperFacetItemResponse> Items { get; set; } = new();
}
