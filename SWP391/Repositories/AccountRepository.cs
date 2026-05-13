using Microsoft.EntityFrameworkCore;
using SWP391.Entities;

namespace SWP391.Repositories
{
    public class AccountRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public AccountRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<User?> GetUserByEmailAsync(string email)
        {
            // [CẬP NHẬT] Thêm .Include(u => u.Roles) để Entity Framework lấy luôn danh sách Role của người dùng này từ bảng trung gian UserRoles.
            return _dbContext.Users
                .Include(u => u.Roles)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            // Nếu bạn muốn cấp quyền tự động lúc đăng ký, bạn có thể lấy Role "Reader" (nếu có trong DB)
            var readerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "Reader");
            
            if (readerRole != null)
            {
                // Assign role "Reader" to new user
                user.Roles.Add(readerRole);
            }

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }
    }
}
