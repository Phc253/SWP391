using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models.Dashboard;

namespace SWP391.Repositories
{
    public class DashboardRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public DashboardRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DashboardSummaryResponse> GetSummaryAsync()
        {
            // Run all independent count queries in parallel to minimise latency
            var totalPapersTask    = _dbContext.Papers.CountAsync();
            var totalKeywordsTask  = _dbContext.Keywords.CountAsync();
            var totalAuthorsTask   = _dbContext.Authors.CountAsync();
            var totalJournalsTask  = _dbContext.Journals.CountAsync();
            var totalUsersTask     = _dbContext.Users.CountAsync();
            var totalTopicsTask    = _dbContext.ResearchTopics.CountAsync();

            // Top 5 keywords by total paper count (all years)
            var topKeywordsTask = _dbContext.Keywords
                .Select(k => new TopKeywordStat
                {
                    Keyword = k.KeywordText,
                    PaperCount = k.Papers.Count()
                })
                .Where(k => k.PaperCount > 0)
                .OrderByDescending(k => k.PaperCount)
                .Take(5)
                .ToListAsync();

            // Papers grouped by publication year (for a simple trend chart on the dashboard)
            var papersByYearTask = _dbContext.Papers
                .Where(p => p.PublicationYear.HasValue)
                .GroupBy(p => p.PublicationYear!.Value)
                .Select(g => new YearPaperCount { Year = g.Key, Count = g.Count() })
                .OrderBy(y => y.Year)
                .ToListAsync();

            // Most recent completed sync time
            var lastSyncTimeTask = _dbContext.SyncJobs
                .Where(j => j.EndTime.HasValue)
                .OrderByDescending(j => j.EndTime)
                .Select(j => j.EndTime)
                .FirstOrDefaultAsync();

            await Task.WhenAll(
                totalPapersTask, totalKeywordsTask, totalAuthorsTask,
                totalJournalsTask, totalUsersTask, totalTopicsTask,
                topKeywordsTask, papersByYearTask, lastSyncTimeTask);

            return new DashboardSummaryResponse
            {
                TotalPapers   = await totalPapersTask,
                TotalKeywords = await totalKeywordsTask,
                TotalAuthors  = await totalAuthorsTask,
                TotalJournals = await totalJournalsTask,
                TotalUsers    = await totalUsersTask,
                TotalTopics   = await totalTopicsTask,
                TopKeywords   = await topKeywordsTask,
                PapersByYear  = await papersByYearTask,
                LastSyncTime  = await lastSyncTimeTask
            };
        }
    }
}
