using Microsoft.EntityFrameworkCore;
using SWP391.Entities;

namespace SWP391.Repositories
{
    public class NotificationRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public NotificationRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Get paginated notifications for a user — unread first, then newest first
        public async Task<List<Notification>> GetByUserIdAsync(int userId, int page, int pageSize)
        {
            return await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderBy(n => n.IsRead)           // false (0) sorts before true (1)
                .ThenByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        // Total count for pagination metadata
        public async Task<int> CountByUserIdAsync(int userId)
        {
            return await _dbContext.Notifications
                .CountAsync(n => n.UserId == userId);
        }

        public async Task<Notification?> GetByIdAsync(long notificationId)
        {
            return await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId);
        }

        public async Task<bool> MarkAsReadAsync(long notificationId)
        {
            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId);

            if (notification == null) return false;

            notification.IsRead = true;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Bulk update — uses ExecuteUpdateAsync to avoid loading all rows into memory
        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            return await _dbContext.Notifications
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }

        public async Task<bool> DeleteAsync(long notificationId)
        {
            var notification = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId);

            if (notification == null) return false;

            _dbContext.Notifications.Remove(notification);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Count unread — useful for badge counts on frontend (included for completeness)
        public async Task<int> CountUnreadAsync(int userId)
        {
            return await _dbContext.Notifications
                .CountAsync(n => n.UserId == userId && n.IsRead == false);
        }
    }
}
