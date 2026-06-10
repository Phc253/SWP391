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

        public async Task<List<Notification>> GetByUserIdAsync(int userId, int page, int pageSize)
        {
            return await _dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

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

        public async Task<List<Notification>> GetExistingByRelatedAsync(
            string relatedType,
            IEnumerable<long> relatedIds,
            IEnumerable<int> userIds)
        {
            var ids = relatedIds.Distinct().ToList();
            var users = userIds.Distinct().ToList();

            if (!ids.Any() || !users.Any())
            {
                return new List<Notification>();
            }

            return await _dbContext.Notifications
                .Where(n =>
                    n.RelatedType == relatedType &&
                    n.RelatedId.HasValue &&
                    ids.Contains(n.RelatedId.Value) &&
                    users.Contains(n.UserId))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddRangeAsync(List<Notification> notifications)
        {
            if (!notifications.Any())
            {
                return;
            }

            await _dbContext.Notifications.AddRangeAsync(notifications);
            await _dbContext.SaveChangesAsync();
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

        public async Task<int> CountUnreadAsync(int userId)
        {
            return await _dbContext.Notifications
                .CountAsync(n => n.UserId == userId && n.IsRead == false);
        }
    }
}
