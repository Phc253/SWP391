using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models.Trend;

namespace SWP391.Repositories
{
    public class TrendRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public TrendRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Keyword?> GetKeywordByIdAsync(int keywordId)
        {
            return await _dbContext.Keywords.FindAsync(keywordId);
        }

        public async Task<List<Keyword>> GetKeywordsByIdsAsync(IEnumerable<int> keywordIds)
        {
            return await _dbContext.Keywords.Where(k => keywordIds.Contains(k.KeywordId)).ToListAsync();
        }

        // FR5: Track publication trends by keyword
        public async Task<List<TrendChartResponse>> GetTrendByKeywordAsync(string keywordText)
        {
            return await _dbContext.Keywords
                .Where(k => k.KeywordText == keywordText)
                .SelectMany(k => k.Papers)
                .Where(p => p.PublicationYear.HasValue)
                .GroupBy(p => p.PublicationYear!.Value)
                .Select(g => new TrendChartResponse
                {
                    Year = g.Key,
                    PaperCount = g.Count()
                })
                .OrderBy(t => t.Year)
                .ToListAsync();
        }

        // FR7: View trending research keywords based on recent papers
        public async Task<List<TrendingTopicResponse>> GetTrendingKeywordsAsync(int topN = 10)
        {
            int currentYear = DateTime.Now.Year;
            int startYear = currentYear - 5; // Xét trong 5 năm gần nhất (vì dl mock có thể cũ)

            return await _dbContext.Keywords
                .Select(k => new TrendingTopicResponse
                {
                    Name = k.KeywordText,
                    RecentPaperCount = k.Papers.Count(p => p.PublicationYear >= startYear),
                    Type = "Keyword"
                })
                .Where(k => k.RecentPaperCount > 0)
                .OrderByDescending(k => k.RecentPaperCount)
                .Take(topN)
                .ToListAsync();
        }
    }
}
