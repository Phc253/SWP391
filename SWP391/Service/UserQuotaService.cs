using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SWP391.Service
{
    public class UserQuotaService
    {
        private readonly ScientificTrendDbContext _dbContext;
        private readonly ILogger<UserQuotaService> _logger;

        // Thread-safe lock to prevent credit overspend or race conditions during check/deduct
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // Fallback default configurations
        private const int DefaultLimitMember = 100;
        private const int DefaultLimitResearcher = 500;
        private const int DefaultLimitAdministrator = 999999;

        private const int DefaultCostEnrichAuthor = 10;
        private const int DefaultCostFetchWork = 10;
        private const int DefaultCostExportReport = 20;

        private const int DefaultGlobalDailyLimit = 10000;

        public UserQuotaService(ScientificTrendDbContext dbContext, ILogger<UserQuotaService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Validates user and global budgets, consumes the required credits if valid.
        /// </summary>
        /// <param name="userId">The ID of the user performing the operation (null for system/scheduled operations).</param>
        /// <param name="operationKey">The operation settings key (e.g. Budget:Cost:EnrichAuthor).</param>
        /// <returns>A ServiceResult indicating success or failure due to budget limits.</returns>
        public async Task<ServiceResult<bool>> CheckAndConsumeBudgetAsync(int? userId, string operationKey)
        {
            await _semaphore.WaitAsync();
            try
            {
                // 1. Determine cost of operation
                int cost = await GetOperationCostAsync(operationKey);
                _logger.LogInformation("Checking budget for operation={Operation} cost={Cost} userId={UserId}", operationKey, cost, userId);

                // 2. Check and consume global daily limit if it's an external OpenAlex API call
                bool isExternalApi = operationKey == "Budget:Cost:EnrichAuthor" || operationKey == "Budget:Cost:FetchWork";
                if (isExternalApi)
                {
                    var globalLimitCheck = await CheckAndConsumeGlobalBudgetAsync(cost);
                    if (!globalLimitCheck.Success)
                    {
                        return ServiceResult<bool>.Fail(globalLimitCheck.Error ?? "Global daily budget limit exceeded.");
                    }
                }

                // 3. Check and consume user quota if userId is provided
                if (userId.HasValue)
                {
                    var user = await _dbContext.Users
                        .Include(u => u.Roles)
                        .FirstOrDefaultAsync(u => u.UserId == userId.Value);

                    if (user == null)
                    {
                        return ServiceResult<bool>.Fail("User not found.");
                    }

                    // Reset user's daily credit if day has passed (comparing UTC dates)
                    DateTime nowUtc = DateTime.UtcNow;
                    if (user.LastCreditResetTime == null || user.LastCreditResetTime.Value.Date < nowUtc.Date)
                    {
                        int dailyLimit = await GetUserDailyLimitAsync(user);
                        user.RemainingCredits = dailyLimit;
                        user.LastCreditResetTime = nowUtc;
                        _logger.LogInformation("Daily credits reset for user={Email} limit={Limit}", user.Email, dailyLimit);
                    }

                    // Administrator bypasses user-level credit checks
                    bool isAdmin = user.Roles.Any(r => string.Equals(r.RoleName, "Administrator", StringComparison.OrdinalIgnoreCase));
                    if (!isAdmin)
                    {
                        int currentCredits = user.RemainingCredits ?? 0;
                        if (currentCredits < cost)
                        {
                            return ServiceResult<bool>.Fail($"Insufficient credits. Operation requires {cost} credits, but you only have {currentCredits} remaining.");
                        }

                        user.RemainingCredits = currentCredits - cost;
                        _logger.LogInformation("User credit deducted for user={Email} cost={Cost} remaining={Remaining}", user.Email, cost, user.RemainingCredits);
                    }
                }

                await _dbContext.SaveChangesAsync();
                return ServiceResult<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking and consuming budget for operation={Operation}", operationKey);
                return ServiceResult<bool>.Fail($"Internal quota check failure: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<int> GetOperationCostAsync(string operationKey)
        {
            var setting = await _dbContext.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == operationKey);

            if (setting != null && int.TryParse(setting.SettingValue, out int val))
            {
                return val;
            }

            // Fallbacks
            return operationKey switch
            {
                "Budget:Cost:EnrichAuthor" => DefaultCostEnrichAuthor,
                "Budget:Cost:FetchWork" => DefaultCostFetchWork,
                "Budget:Cost:ExportReport" => DefaultCostExportReport,
                _ => 1 // default minimal cost
            };
        }

        private async Task<int> GetUserDailyLimitAsync(User user)
        {
            // Determine primary role
            string roleKey = "Budget:Default:Member";
            if (user.Roles.Any(r => string.Equals(r.RoleName, "Administrator", StringComparison.OrdinalIgnoreCase)))
            {
                roleKey = "Budget:Default:Administrator";
            }
            else if (user.Roles.Any(r => string.Equals(r.RoleName, "Researcher", StringComparison.OrdinalIgnoreCase)))
            {
                roleKey = "Budget:Default:Researcher";
            }

            var setting = await _dbContext.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == roleKey);

            if (setting != null && int.TryParse(setting.SettingValue, out int val))
            {
                return val;
            }

            // Fallbacks
            return roleKey switch
            {
                "Budget:Default:Administrator" => DefaultLimitAdministrator,
                "Budget:Default:Researcher" => DefaultLimitResearcher,
                _ => DefaultLimitMember
            };
        }

        private async Task<ServiceResult<bool>> CheckAndConsumeGlobalBudgetAsync(int cost)
        {
            DateTime nowUtc = DateTime.UtcNow;

            // Load settings
            var limitSetting = await _dbContext.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "Budget:GlobalDailyLimit");
            var usedSetting = await _dbContext.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "Budget:GlobalDailyUsed");
            var lastResetSetting = await _dbContext.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "Budget:GlobalDailyLastReset");

            int globalLimit = limitSetting != null && int.TryParse(limitSetting.SettingValue, out int limVal)
                ? limVal
                : DefaultGlobalDailyLimit;

            int globalUsed = 0;
            if (usedSetting != null && int.TryParse(usedSetting.SettingValue, out int usedVal))
            {
                globalUsed = usedVal;
            }

            DateTime lastReset = DateTime.MinValue;
            if (lastResetSetting != null && DateTime.TryParse(lastResetSetting.SettingValue, out var resetVal))
            {
                lastReset = resetVal;
            }

            // Check if global daily reset is needed
            if (lastReset.Date < nowUtc.Date)
            {
                globalUsed = 0;
                lastReset = nowUtc;

                if (usedSetting == null)
                {
                    usedSetting = new SystemSetting { SettingKey = "Budget:GlobalDailyUsed", SettingValue = "0" };
                    _dbContext.SystemSettings.Add(usedSetting);
                }
                else
                {
                    usedSetting.SettingValue = "0";
                }

                if (lastResetSetting == null)
                {
                    lastResetSetting = new SystemSetting { SettingKey = "Budget:GlobalDailyLastReset", SettingValue = nowUtc.ToString("o") };
                    _dbContext.SystemSettings.Add(lastResetSetting);
                }
                else
                {
                    lastResetSetting.SettingValue = nowUtc.ToString("o");
                }

                _logger.LogInformation("Global Daily API quota reset to 0.");
            }

            if (globalUsed + cost > globalLimit)
            {
                _logger.LogWarning("Global Daily API limit reached. Used={Used} Limit={Limit} Required={Required}", globalUsed, globalLimit, cost);
                return ServiceResult<bool>.Fail($"Global Daily API budget exceeded ({globalLimit} maximum). Operation rejected.");
            }

            // Update used
            globalUsed += cost;
            if (usedSetting == null)
            {
                usedSetting = new SystemSetting { SettingKey = "Budget:GlobalDailyUsed", SettingValue = globalUsed.ToString() };
                _dbContext.SystemSettings.Add(usedSetting);
            }
            else
            {
                usedSetting.SettingValue = globalUsed.ToString();
            }

            _logger.LogInformation("Global daily API budget updated. Used={Used} Limit={Limit}", globalUsed, globalLimit);
            return ServiceResult<bool>.Ok(true);
        }
    }
}
