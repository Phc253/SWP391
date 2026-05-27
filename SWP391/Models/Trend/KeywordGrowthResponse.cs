namespace SWP391.Models.Trend
{
    public class KeywordGrowthResponse
    {
        public string KeywordText { get; set; } = null!;
        public List<YearGrowthPoint> GrowthData { get; set; } = new();
    }
}
