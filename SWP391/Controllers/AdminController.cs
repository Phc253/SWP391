using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        // ── User Management ───────────────────────────────────────────────────────────

        // GET: api/admin/users?page=1&pageSize=20
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _adminService.GetUsersAsync(page, pageSize);
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
    }
}
