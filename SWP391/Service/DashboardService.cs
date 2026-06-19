using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Admin;
using SWP391.Models.Dashboard;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class DashboardService
    {
        private readonly DashboardRepository _dashboardRepository;
        private readonly DashboardReportRepository _reportRepository;

        public DashboardService(
            DashboardRepository dashboardRepository,
            DashboardReportRepository reportRepository)
        {
            _dashboardRepository = dashboardRepository;
            _reportRepository    = reportRepository;
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

        public async Task<ServiceResult<UserDashboardResponse>> GetUserSummaryAsync(int userId)
        {
            try
            {
                var data = await _dashboardRepository.GetUserSummaryAsync(userId);
                return ServiceResult<UserDashboardResponse>.Ok(data);
            }
            catch (Exception ex)
            {
                return ServiceResult<UserDashboardResponse>.Fail(
                    "An error occurred while loading your dashboard: " + ex.Message);
            }
        }

        // ── DashboardReport CRUD ──────────────────────────────────────────────────────

        public async Task<ServiceResult<DashboardReportResponse>> SaveReportAsync(
            int userId, SaveReportRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ReportName))
                    return ServiceResult<DashboardReportResponse>.Fail("ReportName is required.");

                if (request.ReportName.Length > 200)
                    return ServiceResult<DashboardReportResponse>.Fail(
                        "ReportName must be 200 characters or fewer.");

                if (string.IsNullOrWhiteSpace(request.ReportType))
                    return ServiceResult<DashboardReportResponse>.Fail("ReportType is required.");

                if (request.ReportType.Length > 50)
                    return ServiceResult<DashboardReportResponse>.Fail(
                        "ReportType must be 50 characters or fewer.");

                var entity = new DashboardReport
                {
                    UserId       = userId,
                    ReportName   = request.ReportName.Trim(),
                    ReportType   = request.ReportType.Trim(),
                    FilterConfig = request.FilterConfig,
                    GeneratedAt  = DateTime.UtcNow
                };

                var created = await _reportRepository.CreateAsync(entity);
                return ServiceResult<DashboardReportResponse>.Ok(MapReport(created));
            }
            catch (Exception ex)
            {
                return ServiceResult<DashboardReportResponse>.Fail(
                    "An error occurred while saving the report: " + ex.Message);
            }
        }

        public async Task<ServiceResult<PagedResponse<DashboardReportResponse>>> GetMyReportsAsync(
            int userId, int page, int pageSize)
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 1, 100);
                page     = Math.Max(1, page);

                var (items, total) = await _reportRepository.GetByUserIdAsync(userId, page, pageSize);

                return ServiceResult<PagedResponse<DashboardReportResponse>>.Ok(
                    new PagedResponse<DashboardReportResponse>
                    {
                        Page       = page,
                        PageSize   = pageSize,
                        TotalCount = total,
                        Items      = items.Select(MapReport).ToList()
                    });
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResponse<DashboardReportResponse>>.Fail(
                    "An error occurred while fetching reports: " + ex.Message);
            }
        }

        public async Task<ServiceResult<DashboardReportResponse>> GetReportByIdAsync(
            int userId, long reportId)
        {
            try
            {
                var report = await _reportRepository.GetByIdAsync(reportId);
                if (report == null)
                    return ServiceResult<DashboardReportResponse>.Fail("Report not found.");

                if (report.UserId != userId)
                    return ServiceResult<DashboardReportResponse>.Fail(
                        "You do not have permission to view this report.");

                return ServiceResult<DashboardReportResponse>.Ok(MapReport(report));
            }
            catch (Exception ex)
            {
                return ServiceResult<DashboardReportResponse>.Fail(
                    "An error occurred while fetching the report: " + ex.Message);
            }
        }

        public async Task<ServiceResult<DashboardReportResponse>> UpdateReportAsync(
            int userId, long reportId, SaveReportRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ReportName))
                    return ServiceResult<DashboardReportResponse>.Fail("ReportName is required.");

                if (request.ReportName.Length > 200)
                    return ServiceResult<DashboardReportResponse>.Fail(
                        "ReportName must be 200 characters or fewer.");

                if (string.IsNullOrWhiteSpace(request.ReportType))
                    return ServiceResult<DashboardReportResponse>.Fail("ReportType is required.");

                if (request.ReportType.Length > 50)
                    return ServiceResult<DashboardReportResponse>.Fail(
                        "ReportType must be 50 characters or fewer.");

                var existing = await _reportRepository.GetByIdAsync(reportId);
                if (existing == null)
                    return ServiceResult<DashboardReportResponse>.Fail("Report not found.");

                if (existing.UserId != userId)
                    return ServiceResult<DashboardReportResponse>.Fail(
                        "You do not have permission to update this report.");

                var updated = await _reportRepository.UpdateAsync(
                    reportId,
                    request.ReportName.Trim(),
                    request.ReportType.Trim(),
                    request.FilterConfig);

                return ServiceResult<DashboardReportResponse>.Ok(MapReport(updated!));
            }
            catch (Exception ex)
            {
                return ServiceResult<DashboardReportResponse>.Fail(
                    "An error occurred while updating the report: " + ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> DeleteReportAsync(int userId, long reportId)
        {
            try
            {
                var existing = await _reportRepository.GetByIdAsync(reportId);
                if (existing == null)
                    return ServiceResult<bool>.Fail("Report not found.");

                if (existing.UserId != userId)
                    return ServiceResult<bool>.Fail(
                        "You do not have permission to delete this report.");

                await _reportRepository.DeleteAsync(reportId);
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(
                    "An error occurred while deleting the report: " + ex.Message);
            }
        }

        private static DashboardReportResponse MapReport(DashboardReport r) => new()
        {
            ReportId     = r.ReportId,
            UserId       = r.UserId,
            ReportName   = r.ReportName,
            ReportType   = r.ReportType,
            FilterConfig = r.FilterConfig,
            GeneratedAt  = r.GeneratedAt
        };
    }
}
