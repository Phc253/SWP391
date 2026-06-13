using SWP391.Models;
using SWP391.Models.Dashboard;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class DashboardService
    {
        private readonly DashboardRepository _dashboardRepository;

        public DashboardService(DashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<ServiceResult<DashboardSummaryResponse>> GetSummaryAsync()
        {
            try
            {
                var summary = await _dashboardRepository.GetSummaryAsync();
                return ServiceResult<DashboardSummaryResponse>.Ok(summary);
            }
            catch (Exception ex)
            {
                return ServiceResult<DashboardSummaryResponse>.Fail(
                    "An error occurred while loading the dashboard: " + ex.Message);
            }
        }
    }
}
