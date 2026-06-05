using SWP391.Models;
using SWP391.Models.Admin;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class AdminService
    {
        private readonly AdminRepository _adminRepository;

        public AdminService(AdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        // ── User Management ───────────────────────────────────────────────────────────

        public async Task<ServiceResult<List<AdminUserResponse>>> GetUsersAsync(int page, int pageSize)
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 1, 100);
                page = Math.Max(1, page);

                var (users, _) = await _adminRepository.GetUsersAsync(page, pageSize);
                var response = users.Select(MapUser).ToList();
                return ServiceResult<List<AdminUserResponse>>.Ok(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<AdminUserResponse>>.Fail(
                    "An error occurred while fetching users: " + ex.Message);
            }
        }

        public async Task<ServiceResult<AdminUserResponse>> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _adminRepository.GetUserByIdAsync(userId);
                if (user == null)
                    return ServiceResult<AdminUserResponse>.Fail($"User {userId} not found.");

                return ServiceResult<AdminUserResponse>.Ok(MapUser(user));
            }
            catch (Exception ex)
            {
                return ServiceResult<AdminUserResponse>.Fail(
                    "An error occurred while fetching the user: " + ex.Message);
            }
        }

        public async Task<ServiceResult<bool>> SetUserActiveAsync(int userId, bool isActive)
        {
            try
            {
                var success = await _adminRepository.SetUserActiveAsync(userId, isActive);
                if (!success)
                    return ServiceResult<bool>.Fail($"User {userId} not found.");

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(
                    "An error occurred while updating user status: " + ex.Message);
            }
        }

        // ── Sync Job History ─────────────────────────────────────────────────────────

        public async Task<ServiceResult<List<SyncJobResponse>>> GetSyncJobsAsync(int page, int pageSize)
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 1, 100);
                page = Math.Max(1, page);

                var (jobs, _) = await _adminRepository.GetSyncJobsAsync(page, pageSize);
                var response = jobs.Select(j => new SyncJobResponse
                {
                    SyncJobId      = j.SyncJobId,
                    SourceName     = j.Source?.SourceName ?? "Unknown",
                    StartTime      = j.StartTime,
                    EndTime        = j.EndTime,
                    Status         = j.Status,
                    RecordsFetched = j.RecordsFetched,
                    ErrorMessage   = j.ErrorMessage
                }).ToList();

                return ServiceResult<List<SyncJobResponse>>.Ok(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<SyncJobResponse>>.Fail(
                    "An error occurred while fetching sync jobs: " + ex.Message);
            }
        }

        // ── System Settings ──────────────────────────────────────────────────────────

        public async Task<ServiceResult<List<SystemSettingResponse>>> GetSettingsAsync()
        {
            try
            {
                var settings = await _adminRepository.GetAllSettingsAsync();
                var response = settings.Select(s => new SystemSettingResponse
                {
                    Key   = s.SettingKey,
                    Value = s.SettingValue
                }).ToList();

                return ServiceResult<List<SystemSettingResponse>>.Ok(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<SystemSettingResponse>>.Fail(
                    "An error occurred while fetching settings: " + ex.Message);
            }
        }

        public async Task<ServiceResult<SystemSettingResponse>> UpdateSettingAsync(string key, string? value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    return ServiceResult<SystemSettingResponse>.Fail("Setting key is required.");

                var setting = await _adminRepository.UpsertSettingAsync(key.Trim(), value);
                return ServiceResult<SystemSettingResponse>.Ok(new SystemSettingResponse
                {
                    Key   = setting.SettingKey,
                    Value = setting.SettingValue
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<SystemSettingResponse>.Fail(
                    "An error occurred while updating the setting: " + ex.Message);
            }
        }

        // ── Private Helpers ───────────────────────────────────────────────────────────

        private static AdminUserResponse MapUser(SWP391.Entities.User user) => new()
        {
            UserId    = user.UserId,
            Email     = user.Email,
            FullName  = user.FullName,
            ActorType = user.ActorType,
            IsActive  = user.IsActive ?? true,
            CreatedAt = user.CreatedAt,
            Roles     = user.Roles.Select(r => r.RoleName).ToList()
        };
    }
}
