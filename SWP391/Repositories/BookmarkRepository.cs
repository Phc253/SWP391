using Microsoft.EntityFrameworkCore;
using SWP391.Entities;

namespace SWP391.Repositories
{
    public class BookmarkRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public BookmarkRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Tìm xem user này đã bookmark đối tượng này (targetId, targetType) chưa
        public async Task<Bookmark?> GetBookmarkAsync(int userId, long targetId, string targetType)
        {
            return await _dbContext.Bookmarks
                .FirstOrDefaultAsync(b => b.UserId == userId && b.TargetId == targetId && b.TargetType == targetType);
        }

        // Lấy danh sách toàn bộ mục đã lưu của user, sắp xếp mới nhất lên đầu
        public async Task<List<Bookmark>> GetUserBookmarksAsync(int userId)
        {
            return await _dbContext.Bookmarks
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        // Lưu bookmark mới vào cơ sở dữ liệu
        public async Task AddBookmarkAsync(Bookmark bookmark)
        {
            await _dbContext.Bookmarks.AddAsync(bookmark);
            await _dbContext.SaveChangesAsync(); // Commit thay đổi xuống DB
        }

        // Xóa một bookmark đã có
        public async Task RemoveBookmarkAsync(Bookmark bookmark)
        {
            _dbContext.Bookmarks.Remove(bookmark);
            await _dbContext.SaveChangesAsync(); // Commit thay đổi xuống DB
        }
    }
}