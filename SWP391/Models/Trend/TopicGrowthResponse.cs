namespace SWP391.Models.Trend
{
    public class TopicGrowthResponse
    {
        public string TopicName { get; set; } = null!;
        public List<YearGrowthPoint> GrowthData { get; set; } = new();
    }
}
