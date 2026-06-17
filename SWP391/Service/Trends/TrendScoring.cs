using SWP391.Repositories;

namespace SWP391.Service.Trends
{
    public static class TrendScoring
    {
        public readonly record struct Score(
            double GrowthRate,
            double Momentum,
            double CitationVelocity,
            double RawScore);

        public static Score Compute(EnhancedActivityRawData item)
        {
            double growthRate = item.BaselinePaperCount == 0
                ? (item.RecentPaperCount > 0 ? 100.0 : 0.0)
                : Math.Round((double)(item.RecentPaperCount - item.BaselinePaperCount)
                             / item.BaselinePaperCount * 100, 2);

            double priorGrowthRate = item.PriorPaperCount == 0
                ? (item.BaselinePaperCount > 0 ? 100.0 : 0.0)
                : Math.Round((double)(item.BaselinePaperCount - item.PriorPaperCount)
                             / item.PriorPaperCount * 100, 2);

            double momentum = Math.Round(growthRate - priorGrowthRate, 2);

            double citationVelocity = Math.Round(
                (double)item.TotalRecentCitations / Math.Max(1, item.RecentPaperCount), 2);

            double rawScore = item.RecentPaperCount * 1.0
                            + growthRate            * 0.5
                            + citationVelocity      * 0.3
                            + momentum              * 0.2;

            return new Score(growthRate, momentum, citationVelocity, rawScore);
        }

        public static double Normalize(double rawScore, double maxRawScore) =>
            Math.Round(rawScore / Math.Max(1.0, maxRawScore) * 100, 2);
    }
}
