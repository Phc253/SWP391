using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Account;
using SWP391.Models.Admin;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class AdminService
    {
        private readonly AdminRepository _adminRepository;
        private readonly ActivityLogService _activityLogService;
        private readonly ScientificTrendDbContext _dbContext;

        public AdminService(
            AdminRepository adminRepository,
            ActivityLogService activityLogService,
            ScientificTrendDbContext dbContext)
        {
            _adminRepository = adminRepository;
            _activityLogService = activityLogService;
            _dbContext = dbContext;
        }

        // ── User Management ───────────────────────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<AdminUserResponse>>> GetUsersAsync(
            int page, int pageSize, string? search = null, int? roleId = null)
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 1, 100);
                page = Math.Max(1, page);

                var (users, total) = await _adminRepository.GetUsersAsync(page, pageSize, search, roleId);
                return ServiceResult<PagedResponse<AdminUserResponse>>.Ok(new PagedResponse<AdminUserResponse>
                {
                    Page       = page,
                    PageSize   = pageSize,
                    TotalCount = total,
                    Items      = users.Select(MapUser).ToList()
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResponse<AdminUserResponse>>.Fail(
                    "An error occurred while fetching users: " + ex.Message);
            }
        }

        public async Task<ServiceResult<List<RoleResponse>>> GetRolesAsync()
        {
            try
            {
                var roles = await _adminRepository.GetRolesAsync();
                var response = roles.Select(r => new RoleResponse
                {
                    RoleId   = r.RoleId,
                    RoleName = r.RoleName
                }).ToList();
                return ServiceResult<List<RoleResponse>>.Ok(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<RoleResponse>>.Fail(
                    "An error occurred while fetching roles: " + ex.Message);
            }
        }

        public async Task<ServiceResult<AdminUserResponse>> CreateUserAsync(CreateUserRequest request)
        {
            try
            {
                var email = request.Email?.Trim() ?? string.Empty;
                var password = request.Password ?? string.Empty;

                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<AdminUserResponse>.Fail("Email is required.");

                if (!IsValidEmail(email))
                    return ServiceResult<AdminUserResponse>.Fail("Email is invalid.");

                if (password.Length < 6)
                    return ServiceResult<AdminUserResponse>.Fail("Password must be at least 6 characters.");

                if (password.Length > 100)
                    return ServiceResult<AdminUserResponse>.Fail("Password is too long.");

                var fullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();
                if (fullName != null && fullName.Length > 150)
                    return ServiceResult<AdminUserResponse>.Fail("Full name is too long.");

                var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
                if (phoneNumber != null && phoneNumber.Length > 20)
                    return ServiceResult<AdminUserResponse>.Fail("Phone number is too long.");

                if (request.DateOfBirth.HasValue && request.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
                    return ServiceResult<AdminUserResponse>.Fail("Date of birth cannot be in the future.");

                var actorType = UserActorTypes.Normalize(request.ActorType);
                if (string.IsNullOrEmpty(actorType))
                    return ServiceResult<AdminUserResponse>.Fail("ActorType must be one of: Researcher, Lecturer, Student.");

                var existing = await _adminRepository.GetUserByEmailAsync(email);
                if (existing != null)
                    return ServiceResult<AdminUserResponse>.Fail("Email already exists.");

                var user = new User
                {
                    Email        = email,
                    PasswordHash = HashPassword(password),
                    FullName     = fullName,
                    DateOfBirth  = request.DateOfBirth?.Date,
                    PhoneNumber  = phoneNumber,
                    ActorType    = actorType,
                    CreatedAt    = DateTime.UtcNow,
                    IsActive     = true   // admin-created accounts are pre-activated
                };

                var created = await _adminRepository.CreateUserAsync(user, request.RoleId);

                await _activityLogService.LogAsync(
                    userId: null,
                    action: "AdminCreatedUser",
                    targetType: "User",
                    targetId: created.UserId,
                    details: $"Email={created.Email}, RoleId={request.RoleId}");

                return ServiceResult<AdminUserResponse>.Ok(MapUser(created));
            }
            catch (Exception ex)
            {
                return ServiceResult<AdminUserResponse>.Fail(
                    "An error occurred while creating the user: " + ex.Message);
            }
        }

        private static bool IsValidEmail(string email)
        {
            try { return new MailAddress(email).Address == email; }
            catch { return false; }
        }

        private static string HashPassword(string password)
        {
            var salt = new byte[16];
            RandomNumberGenerator.Fill(salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
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

                await _activityLogService.LogAsync(
                    userId: null,
                    action: isActive ? "UserActivated" : "UserDeactivated",
                    targetType: "User",
                    targetId: userId,
                    details: $"UserId={userId} set IsActive={isActive}");

                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Fail(
                    "An error occurred while updating user status: " + ex.Message);
            }
        }

        // ── Sync Job History ─────────────────────────────────────────────────────────

        public async Task<ServiceResult<PagedResponse<SyncJobResponse>>> GetSyncJobsAsync(int page, int pageSize)
        {
            try
            {
                pageSize = Math.Clamp(pageSize, 1, 100);
                page = Math.Max(1, page);

                var (jobs, total) = await _adminRepository.GetSyncJobsAsync(page, pageSize);
                var items = jobs.Select(j => new SyncJobResponse
                {
                    SyncJobId      = j.SyncJobId,
                    SourceName     = j.Source?.SourceName ?? "Unknown",
                    StartTime      = j.StartTime,
                    EndTime        = j.EndTime,
                    Status         = j.Status,
                    RecordsFetched = j.RecordsFetched,
                    ErrorMessage   = j.ErrorMessage
                }).ToList();

                return ServiceResult<PagedResponse<SyncJobResponse>>.Ok(new PagedResponse<SyncJobResponse>
                {
                    Page       = page,
                    PageSize   = pageSize,
                    TotalCount = total,
                    Items      = items
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResponse<SyncJobResponse>>.Fail(
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

                await _activityLogService.LogAsync(
                    userId: null,
                    action: "SettingUpdated",
                    targetType: "SystemSetting",
                    details: $"Key={setting.SettingKey}, Value={setting.SettingValue}");

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

        // ── Scheduler Configuration ───────────────────────────────────────────────────

        private const string SchedulerEnabledKey     = "DataSync:Enabled";
        private const string SchedulerKeywordKey     = "DataSync:Keyword";
        private const string SchedulerMaxResultsKey  = "DataSync:MaxResults";
        private const string SchedulerIntervalKey    = "DataSync:IntervalHours";

        public async Task<ServiceResult<SchedulerConfigResponse>> GetSchedulerConfigAsync()
        {
            try
            {
                var settings = await _adminRepository.GetAllSettingsAsync();
                var map = settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

                var config = new SchedulerConfigResponse
                {
                    Enabled       = map.TryGetValue(SchedulerEnabledKey, out var en) && bool.TryParse(en, out var enVal) ? enVal : false,
                    Keyword       = map.TryGetValue(SchedulerKeywordKey, out var kw) && !string.IsNullOrWhiteSpace(kw) ? kw : "Computer Science",
                    MaxResults    = map.TryGetValue(SchedulerMaxResultsKey, out var mr) && int.TryParse(mr, out var mrVal) ? mrVal : 20,
                    IntervalHours = map.TryGetValue(SchedulerIntervalKey, out var ih) && int.TryParse(ih, out var ihVal) ? ihVal : 24
                };

                return ServiceResult<SchedulerConfigResponse>.Ok(config);
            }
            catch (Exception ex)
            {
                return ServiceResult<SchedulerConfigResponse>.Fail(
                    "An error occurred while fetching scheduler config: " + ex.Message);
            }
        }

        public async Task<ServiceResult<SchedulerConfigResponse>> UpdateSchedulerConfigAsync(SchedulerConfigRequest request)
        {
            try
            {
                if (request.Enabled.HasValue)
                    await _adminRepository.UpsertSettingAsync(SchedulerEnabledKey, request.Enabled.Value.ToString().ToLower());

                if (!string.IsNullOrWhiteSpace(request.Keyword))
                    await _adminRepository.UpsertSettingAsync(SchedulerKeywordKey, request.Keyword.Trim());

                if (request.MaxResults.HasValue)
                    await _adminRepository.UpsertSettingAsync(SchedulerMaxResultsKey,
                        Math.Clamp(request.MaxResults.Value, 1, 200).ToString());

                if (request.IntervalHours.HasValue)
                    await _adminRepository.UpsertSettingAsync(SchedulerIntervalKey,
                        Math.Clamp(request.IntervalHours.Value, 1, 720).ToString());

                await _activityLogService.LogAsync(
                    userId: null,
                    action: "SchedulerConfigUpdated",
                    details: $"Enabled={request.Enabled}, Keyword={request.Keyword}, MaxResults={request.MaxResults}, IntervalHours={request.IntervalHours}");

                return await GetSchedulerConfigAsync();
            }
            catch (Exception ex)
            {
                return ServiceResult<SchedulerConfigResponse>.Fail(
                    "An error occurred while updating scheduler config: " + ex.Message);
            }
        }

        public Task<ServiceResult<SchedulerConfigResponse>> SetSchedulerEnabledAsync(bool enabled)
        {
            return UpdateSchedulerConfigAsync(new SchedulerConfigRequest
            {
                Enabled = enabled
            });
        }

        // ── Role Assignment ───────────────────────────────────────────────────────────

        public async Task<ServiceResult<AdminUserResponse>> AssignRoleAsync(int userId, int roleId)
        {
            try
            {
                var user = await _adminRepository.AssignRoleAsync(userId, roleId);
                if (user == null)
                    return ServiceResult<AdminUserResponse>.Fail($"User {userId} or role {roleId} not found.");

                await _activityLogService.LogAsync(
                    userId: null,
                    action: "RoleAssigned",
                    targetType: "User",
                    targetId: userId,
                    details: $"RoleId={roleId} assigned to UserId={userId}");

                return ServiceResult<AdminUserResponse>.Ok(MapUser(user));
            }
            catch (Exception ex)
            {
                return ServiceResult<AdminUserResponse>.Fail(
                    "An error occurred while assigning role: " + ex.Message);
            }
        }

        public async Task<ServiceResult<AdminUserResponse>> RemoveRoleAsync(int userId, int roleId)
        {
            try
            {
                var user = await _adminRepository.RemoveRoleAsync(userId, roleId);
                if (user == null)
                    return ServiceResult<AdminUserResponse>.Fail($"User {userId} not found.");

                await _activityLogService.LogAsync(
                    userId: null,
                    action: "RoleRemoved",
                    targetType: "User",
                    targetId: userId,
                    details: $"RoleId={roleId} removed from UserId={userId}");

                return ServiceResult<AdminUserResponse>.Ok(MapUser(user));
            }
            catch (Exception ex)
            {
                return ServiceResult<AdminUserResponse>.Fail(
                    "An error occurred while removing role: " + ex.Message);
            }
        }

        // ── Admin Stats Dashboard ─────────────────────────────────────────────────────

        // Returns an operational overview: user health, sync job health (last 30 days),
        // notification total, and recent activity. All queries run in parallel.
        // SyncJob status strings used by DataSyncService: "Completed", "CompletedWithWarnings", "Failed"
        // EF Core DbContext is not thread-safe — queries must be sequential, not Task.WhenAll.
        public async Task<ServiceResult<AdminStatsResponse>> GetAdminStatsAsync()
        {
            try
            {
                var cutoff30 = DateTime.UtcNow.AddDays(-30);
                var cutoff7  = DateTime.UtcNow.AddDays(-7);

                var activeUsers   = await _dbContext.Users.CountAsync(u => u.IsActive == true);
                var inactiveUsers = await _dbContext.Users.CountAsync(u => u.IsActive != true);
                var totalNotif    = await _dbContext.Notifications.CountAsync();
                var logsLast7     = await _dbContext.ActivityLogs.CountAsync(a => a.CreatedAt >= cutoff7);
                var lastSync      = await _dbContext.SyncJobs
                                        .Where(j => j.EndTime.HasValue)
                                        .OrderByDescending(j => j.EndTime)
                                        .Select(j => j.EndTime)
                                        .FirstOrDefaultAsync();
                var jobs30        = await _dbContext.SyncJobs
                                        .Where(j => j.StartTime >= cutoff30)
                                        .ToListAsync();
                var recentJobs    = await _dbContext.SyncJobs
                                        .Include(j => j.Source)
                                        .OrderByDescending(j => j.StartTime)
                                        .Take(5)
                                        .AsNoTracking()
                                        .ToListAsync();
                var roleCounts    = await _dbContext.Roles
                                        .Select(r => new RoleCountItem
                                        {
                                            RoleName = r.RoleName,
                                            Count    = r.Users.Count()
                                        })
                                        .ToListAsync();

                var response = new AdminStatsResponse
                {
                    ActiveUsers                   = activeUsers,
                    InactiveUsers                 = inactiveUsers,
                    UsersByRole                   = roleCounts,
                    SyncJobsLast30Days            = jobs30.Count,
                    SyncJobsCompleted             = jobs30.Count(j => j.Status == "Completed"),
                    SyncJobsCompletedWithWarnings = jobs30.Count(j => j.Status == "CompletedWithWarnings"),
                    SyncJobsFailed                = jobs30.Count(j => j.Status == "Failed"),
                    RecentSyncJobs                = recentJobs.Select(j => new RecentSyncJobItem
                    {
                        SyncJobId      = j.SyncJobId,
                        SourceName     = j.Source?.SourceName ?? "Unknown",
                        StartTime      = j.StartTime,
                        EndTime        = j.EndTime,
                        Status         = j.Status,
                        RecordsFetched = j.RecordsFetched,
                        ErrorMessage   = j.ErrorMessage
                    }).ToList(),
                    TotalNotifications    = totalNotif,
                    ActivityLogsLast7Days = logsLast7,
                    LastSyncTime          = lastSync
                };

                return ServiceResult<AdminStatsResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                return ServiceResult<AdminStatsResponse>.Fail(
                    "An error occurred while fetching admin stats: " + ex.Message);
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
