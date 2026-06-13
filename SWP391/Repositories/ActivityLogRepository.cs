using Microsoft.EntityFrameworkCore;
using SWP391.Entities;

namespace SWP391.Repositories
{
    public class ActivityLogRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public ActivityLogRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(ActivityLog log)
        {
            _dbContext.ActivityLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<(List<ActivityLog> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, int? userId, string? action)
        {
            var query = _dbContext.ActivityLogs
                .Include(a => a.User)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action == action);

            query = query.OrderByDescending(a => a.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, total);
        }
    }
}
