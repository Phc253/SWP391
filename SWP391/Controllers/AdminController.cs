using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SWP391.Models.Admin;
using SWP391.Service;

namespace SWP391.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]  // All endpoints in this controller require Administrator role
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly ActivityLogService _activityLogService;

        public AdminController(AdminService adminService, ActivityLogService activityLogService)
        {
            _adminService = adminService;
            _activityLogService = activityLogService;
        }

        // ── User Management ───────────────────────────────────────────────────────────

        // GET: api/admin/users?page=1&pageSize=20&search=&roleId=
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] int? roleId = null)
        {
            var result = await _adminService.GetUsersAsync(page, pageSize, search, roleId);
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }

        // POST: api/admin/users
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var result = await _adminService.CreateUserAsync(request);
            if (!result.Success)
            {
                if (result.Error!.Contains("already exists") || result.Error.Contains("invalid") ||
                    result.Error.Contains("required") || result.Error.Contains("too long") ||
                    result.Error.Contains("future") || result.Error.Contains("ActorType"))
                    return BadRequest(result);
                return StatusCode(500, result);
            }

            return CreatedAtAction(nameof(GetUserById), new { id = result.Data!.UserId }, result.Data);
        }

        // GET: api/admin/roles
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var result = await _adminService.GetRolesAsync();
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }

        // GET: api/admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var result = await _adminService.GetUserByIdAsync(id);
            if (!result.Success)
                return NotFound(result);

            return Ok(result.Data);
        }

        // PATCH: api/admin/users/{id}/deactivate
        [HttpPatch("users/{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var result = await _adminService.SetUserActiveAsync(id, false);
            if (!result.Success)
                return NotFound(result);

            return Ok(new { message = $"User {id} has been deactivated." });
        }

        // PATCH: api/admin/users/{id}/activate
        [HttpPatch("users/{id}/activate")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var result = await _adminService.SetUserActiveAsync(id, true);
            if (!result.Success)
                return NotFound(result);

            return Ok(new { message = $"User {id} has been activated." });
        }

        // ── Sync Job History ─────────────────────────────────────────────────────────

        // GET: api/admin/sync-jobs?page=1&pageSize=20
        [HttpGet("sync-jobs")]
        public async Task<IActionResult> GetSyncJobs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetSyncJobsAsync(page, pageSize);
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }

        // ── System Settings ──────────────────────────────────────────────────────────

        // GET: api/admin/settings
        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var result = await _adminService.GetSettingsAsync();
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }

        // PUT: api/admin/settings/{key}
        // Body: { "value": "new-value" }
        // Upserts the setting: creates it if the key doesn't exist, updates it otherwise.
        [HttpPut("settings/{key}")]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request)
        {
            var result = await _adminService.UpdateSettingAsync(key, request.Value);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data);
        }

        // ── Activity Logs ─────────────────────────────────────────────────────────────

        // GET: api/admin/activity-logs?page=1&pageSize=20&userId=&action=
        [HttpGet("activity-logs")]
        public async Task<IActionResult> GetActivityLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] int? userId = null,
            [FromQuery] string? action = null)
        {
            var result = await _activityLogService.GetLogsAsync(page, pageSize, userId, action);
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }

        // ── Scheduler Configuration ───────────────────────────────────────────────────

        // GET: api/admin/scheduler-config
        [HttpGet("scheduler-config")]
        public async Task<IActionResult> GetSchedulerConfig()
        {
            var result = await _adminService.GetSchedulerConfigAsync();
            if (!result.Success)
                return StatusCode(500, result);

            return Ok(result.Data);
        }

        // PUT: api/admin/scheduler-config
        [HttpPut("scheduler-config")]
        public async Task<IActionResult> UpdateSchedulerConfig([FromBody] SchedulerConfigRequest request)
        {
            var result = await _adminService.UpdateSchedulerConfigAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data);
        }

        // PATCH: api/admin/scheduler-config/enable
        // Saves optional scheduler settings and enables the scheduler in one request.
        [HttpPatch("scheduler-config/enable")]
        public async Task<IActionResult> EnableScheduler(
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SchedulerConfigRequest? request = null)
        {
            request ??= new SchedulerConfigRequest();
            request.Enabled = true;

            var result = await _adminService.UpdateSchedulerConfigAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data);
        }

        // PATCH: api/admin/scheduler-config/disable
        [HttpPatch("scheduler-config/disable")]
        public async Task<IActionResult> DisableScheduler()
        {
            var result = await _adminService.SetSchedulerEnabledAsync(false);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result.Data);
        }

        // ── Role Assignment ───────────────────────────────────────────────────────────

        // POST: api/admin/users/{id}/roles
        // Body: { "roleId": 2 }
        [HttpPost("users/{id}/roles")]
        public async Task<IActionResult> AssignRole(int id, [FromBody] AssignRoleRequest request)
        {
            var result = await _adminService.AssignRoleAsync(id, request.RoleId);
            if (!result.Success)
            {
                if (result.Error!.Contains("not found"))
                    return NotFound(result);
                return BadRequest(result);
            }

            return Ok(result.Data);
        }

        // DELETE: api/admin/users/{id}/roles/{roleId}
        [HttpDelete("users/{id}/roles/{roleId}")]
        public async Task<IActionResult> RemoveRole(int id, int roleId)
        {
            var result = await _adminService.RemoveRoleAsync(id, roleId);
            if (!result.Success)
            {
                if (result.Error!.Contains("not found"))
                    return NotFound(result);
                return BadRequest(result);
            }

            return Ok(result.Data);
        }

        // ── Admin Stats ───────────────────────────────────────────────────────────────

        // GET: api/admin/stats
        // Operational overview: user health, sync job health (last 30 days), recent activity.
        [HttpGet("stats")]
        public async Task<IActionResult> GetAdminStats()
        {
            var result = await _adminService.GetAdminStatsAsync();
            if (!result.Success)
                return StatusCode(500, new { error = result.Error });

            return Ok(result.Data);
        }
    }
}
