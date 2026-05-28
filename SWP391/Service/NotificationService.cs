using SWP391.Models;
using SWP391.Models.Notification;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class NotificationService
    {
        private readonly NotificationRepository _notificationRepository;

        public NotificationService(NotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<ServiceResult<List<NotificationResponse>>> GetNotificationsAsync(
            int userId, int page, int pageSize)
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 1, 100);
                page = Math.Max(1, page);

                var notifications = await _notificationRepository.GetByUserIdAsync(userId, page, pageSize);
                var total = await _notificationRepository.CountByUserIdAsync(userId);

                var response = notifications.Select(n => new NotificationResponse
                {
                    NotificationId = n.NotificationId,
                    Message        = n.Message,
                    IsRead         = n.IsRead ?? false,
                    CreatedAt      = n.CreatedAt,
                    RelatedId      = n.RelatedId,
                    RelatedType    = n.RelatedType
                }).ToList();

                return ServiceResult<List<NotificationResponse>>.Ok(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<NotificationResponse>>.Fail(
                    "An error occurred while fetching notifications: " + ex.Message);
            }
        }

        // Mark a single notification as read — enforces ownership so users cannot
        // mark other users' notifications.
        public async Task<ServiceResult<bool>> MarkAsReadAsync(int userId, long notificationId)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification == null)
                    return ServiceResult<bool>.Fail("Notification not found.");

                if (notification.UserId != userId)
                    return ServiceResult<bool>.Fail("You do not have permission to update this notification.");

                await _notificationRepository.MarkAsReadAsync(notificationId);
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(
                    "An error occurred while marking the notification as read: " + ex.Message);
            }
        }

        // Mark all notifications for the current user as read.
        public async Task<ServiceResult<bool>> MarkAllAsReadAsync(int userId)
        {
            try
            {
                await _notificationRepository.MarkAllAsReadAsync(userId);
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(
                    "An error occurred while marking all notifications as read: " + ex.Message);
            }
        }

        // Delete a notification — enforces ownership.
        public async Task<ServiceResult<bool>> DeleteAsync(int userId, long notificationId)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(notificationId);
                if (notification == null)
                    return ServiceResult<bool>.Fail("Notification not found.");

                if (notification.UserId != userId)
                    return ServiceResult<bool>.Fail("You do not have permission to delete this notification.");

                await _notificationRepository.DeleteAsync(notificationId);
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(
                    "An error occurred while deleting the notification: " + ex.Message);
            }
        }
    }
}
