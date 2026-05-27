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

        public async Task<(List<User> Users, int TotalCount)> GetUsersAsync(int page, int pageSize)
        {
            var query = _dbContext.Users
                .Include(u => u.Roles)
                .OrderBy(u => u.UserId);

            var total = await query.CountAsync();
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (users, total);
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
    }
}
