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

        public async Task<User> CreateUserAsync(User user, int? requestedRoleId = null)
        {
            // Nếu có specified RoleId (từ Client chọn qua combobox), thử lấy role đó lên
            Role? roleToAssign = null;
            if (requestedRoleId.HasValue)
            {
                roleToAssign = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleId == requestedRoleId.Value);
            }
            
            // Nếu không có role được yêu cầu hoặc role không tồn tại, lấy role mặc định (vd: Member)
            if (roleToAssign == null)
            {
                roleToAssign = await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "Member");
            }
            
            if (roleToAssign != null)
            {
                user.Roles.Add(roleToAssign);
            }

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }

        public async Task AddEmailVerificationTokenAsync(EmailVerificationToken token)
        {
            _dbContext.EmailVerificationTokens.Add(token);
            await _dbContext.SaveChangesAsync();
        }

        public Task<EmailVerificationToken?> GetValidEmailVerificationTokenAsync(string tokenHash)
        {
            var now = DateTime.UtcNow;

            return _dbContext.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.TokenHash == tokenHash &&
                    t.UsedAt == null &&
                    t.ExpiresAt > now);
        }

        public async Task MarkEmailVerifiedAsync(EmailVerificationToken token)
        {
            token.UsedAt = DateTime.UtcNow;
            token.User.IsActive = true;
            await _dbContext.SaveChangesAsync();
        }

        public Task<bool> IsTokenRevokedAsync(string tokenHash)
        {
            var now = DateTime.UtcNow;

            return _dbContext.RevokedTokens
                .AsNoTracking()
                .AnyAsync(t => t.TokenHash == tokenHash && t.ExpiresAt > now);
        }

        public async Task RevokeTokenAsync(string tokenHash, int userId, DateTime expiresAt)
        {
            var now = DateTime.UtcNow;
            var exists = await _dbContext.RevokedTokens
                .AnyAsync(t => t.TokenHash == tokenHash);

            if (exists)
            {
                return;
            }

            _dbContext.RevokedTokens.Add(new RevokedToken
            {
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                RevokedAt = now
            });

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _dbContext.ChangeTracker.Clear();
                var wasInsertedConcurrently = await _dbContext.RevokedTokens
                    .AsNoTracking()
                    .AnyAsync(t => t.TokenHash == tokenHash);

                if (!wasInsertedConcurrently)
                {
                    throw;
                }
            }
        }
    }
}
