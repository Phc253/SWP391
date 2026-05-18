using SWP391.Models;
using SWP391.Models.Trend;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class TrendService
    {
        private readonly TrendRepository _trendRepository;

        public TrendService(TrendRepository trendRepository)
        {
            _trendRepository = trendRepository;
        }

        public async Task<ServiceResult<List<TrendChartResponse>>> GetKeywordTrendAsync(string keywordText)
        {
            try
            {
                var data = await _trendRepository.GetTrendByKeywordAsync(keywordText);
                if (!data.Any())
                {
                    return ServiceResult<List<TrendChartResponse>>.Fail("No data found for this keyword. It might not exist or has no papers.");
                }
                
                return ServiceResult<List<TrendChartResponse>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<TrendChartResponse>>.Fail("An error occurred while fetching keyword trend: " + ex.Message);
            }
        }

        public async Task<ServiceResult<List<TrendingTopicResponse>>> GetTrendingTopicsAsync(int topN = 10)
        {
            try
            {
                var data = await _trendRepository.GetTrendingKeywordsAsync(topN);
                return ServiceResult<List<TrendingTopicResponse>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<TrendingTopicResponse>>.Fail("An error occurred while fetching trending topics: " + ex.Message);
            }
        }
    }
}
