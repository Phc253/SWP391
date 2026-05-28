namespace SWP391.Models.Trend
{
    public class YearGrowthPoint
    {
        public int Year { get; set; }
        public int PaperCount { get; set; }
        public double? GrowthRate { get; set; }  // null for first year in the requested window
    }
}
