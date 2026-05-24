using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP391.Repositories
{
    public class FollowRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public FollowRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Tìm xem user này đã follow đối tượng này (targetId, targetType) chưa
        public async Task<Follow?> GetFollowAsync(int userId, long targetId, string targetType)
        {
            return await _dbContext.Follows
                .FirstOrDefaultAsync(f => f.UserId == userId && f.TargetId == targetId && f.TargetType == targetType);
        }

        // Kiểm tra xem user này đã follow đối tượng này chưa (trả về boolean)
        public async Task<bool> IsFollowingAsync(int userId, long targetId, string targetType)
        {
            return await _dbContext.Follows
                .AnyAsync(f => f.UserId == userId && f.TargetId == targetId && f.TargetType == targetType);
        }

        // Lấy danh sách toàn bộ mục đã follow của user, sắp xếp mới nhất lên đầu
        public async Task<List<Follow>> GetUserFollowsAsync(int userId)
        {
            return await _dbContext.Follows
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        // Thêm follow mới
        public async Task AddFollowAsync(Follow follow)
        {
            await _dbContext.Follows.AddAsync(follow);
            await _dbContext.SaveChangesAsync(); // Commit xuống DB
        }

        // Xóa follow
        public async Task RemoveFollowAsync(Follow follow)
        {
            _dbContext.Follows.Remove(follow);
            await _dbContext.SaveChangesAsync(); // Commit xuống DB
        }
    }
}
