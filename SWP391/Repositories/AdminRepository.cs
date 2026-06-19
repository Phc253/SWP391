using Microsoft.EntityFrameworkCore;
using SWP391.Entities;

namespace SWP391.Repositories
{
    public class AdminRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public AdminRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ── User Management ───────────────────────────────────────────────────────────

        public async Task<(List<User> Users, int TotalCount)> GetUsersAsync(
            int page, int pageSize, string? search = null, int? roleId = null)
        {
            var query = _dbContext.Users
                .Include(u => u.Roles)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(term) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(term)));
            }

            if (roleId.HasValue)
                query = query.Where(u => u.Roles.Any(r => r.RoleId == roleId.Value));

            query = query.OrderBy(u => u.UserId);

            var total = await query.CountAsync();
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (users, total);
        }

        public async Task<List<Role>> GetRolesAsync()
        {
            return await _dbContext.Roles
                .OrderBy(r => r.RoleId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<User> CreateUserAsync(User user, int? roleId)
        {
            Role? role = null;
            if (roleId.HasValue)
                role = await _dbContext.Roles.FindAsync(roleId.Value);
            role ??= await _dbContext.Roles.FirstOrDefaultAsync(r => r.RoleName == "Member");

            if (role != null)
                user.Roles.Add(role);

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            await _dbContext.Entry(user).Collection(u => u.Roles).LoadAsync();
            return user;
        }

        public Task<User?> GetUserByEmailAsync(string email)
        {
            return _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _dbContext.Users
                .Include(u => u.Roles)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<bool> SetUserActiveAsync(int userId, bool isActive)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return false;

            user.IsActive = isActive;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // ── Sync Job History ─────────────────────────────────────────────────────────

        public async Task<(List<SyncJob> Jobs, int TotalCount)> GetSyncJobsAsync(int page, int pageSize)
        {
            var query = _dbContext.SyncJobs
                .Include(j => j.Source)
                .OrderByDescending(j => j.StartTime);  // newest first

            var total = await query.CountAsync();
            var jobs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (jobs, total);
        }

        // ── System Settings ──────────────────────────────────────────────────────────

        public async Task<List<SystemSetting>> GetAllSettingsAsync()
        {
            return await _dbContext.SystemSettings
                .OrderBy(s => s.SettingKey)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<SystemSetting> UpsertSettingAsync(string key, string? value)
        {
            var setting = await _dbContext.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);

            if (setting == null)
            {
                setting = new SystemSetting { SettingKey = key, SettingValue = value };
                _dbContext.SystemSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = value;
            }

            await _dbContext.SaveChangesAsync();
            return setting;
        }

        // ── Role Assignment ──────────────────────────────────────────────────────────

        public async Task<User?> AssignRoleAsync(int userId, int roleId)
        {
            var user = await _dbContext.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            var role = await _dbContext.Roles.FindAsync(roleId);
            if (role == null) return null;

            if (!user.Roles.Any(r => r.RoleId == roleId))
            {
                user.Roles.Add(role);
                await _dbContext.SaveChangesAsync();
            }

            return user;
        }

        public async Task<User?> RemoveRoleAsync(int userId, int roleId)
        {
            var user = await _dbContext.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            var role = user.Roles.FirstOrDefault(r => r.RoleId == roleId);
            if (role != null)
            {
                user.Roles.Remove(role);
                await _dbContext.SaveChangesAsync();
            }

            return user;
        }
    }
}
