using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Admin;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class ActivityLogService
    {
        private readonly ActivityLogRepository _activityLogRepository;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(
            ActivityLogRepository activityLogRepository,
            ILogger<ActivityLogService> logger)
        {
            _activityLogRepository = activityLogRepository;
            _logger = logger;
        }

        // Fire-and-forget — exceptions are swallowed so logging never breaks callers.
        public async Task LogAsync(
            int? userId,
            string action,
            string? targetType = null,
            long? targetId = null,
            string? details = null,
            string? ipAddress = null)
        {
            try
            {
                var log = new ActivityLog
                {
                    UserId     = userId,
                    Action     = action,
                    TargetType = targetType,
                    TargetId   = targetId,
                    Details    = details,
                    IpAddress  = ipAddress,
                    CreatedAt  = DateTime.UtcNow
                };
                await _activityLogRepository.AddAsync(log);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ActivityLog write failed for action {Action}.", action);
            }
        }

        public async Task<ServiceResult<PagedActivityLogResponse>> GetLogsAsync(
            int page, int pageSize, int? userId, string? action)
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 1, 100);
                page = Math.Max(1, page);

                var (items, total) = await _activityLogRepository.GetPagedAsync(page, pageSize, userId, action);

                var response = new PagedActivityLogResponse
                {
                    Page       = page,
                    PageSize   = pageSize,
                    TotalCount = total,
                    Items      = items.Select(a => new ActivityLogItem
                    {
                        ActivityLogId = a.ActivityLogId,
                        UserId        = a.UserId,
                        UserEmail     = a.User?.Email,
                        Action        = a.Action,
                        TargetType    = a.TargetType,
                        TargetId      = a.TargetId,
                        Details       = a.Details,
                        IpAddress     = a.IpAddress,
                        CreatedAt     = a.CreatedAt
                    }).ToList()
                };

                return ServiceResult<PagedActivityLogResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedActivityLogResponse>.Fail(
                    "An error occurred while fetching activity logs: " + ex.Message);
            }
        }
    }
}
