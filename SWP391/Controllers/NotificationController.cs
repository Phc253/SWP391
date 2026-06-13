using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP391.Service;
using System.Security.Claims;

namespace SWP391.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "IsMember")]  // All notification endpoints require a valid member JWT
    public class NotificationController : ControllerBase
    {
        private readonly NotificationService _notificationService;

        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // Extracts the authenticated user's ID from JWT claims.
        // UserId is set as ClaimTypes.NameIdentifier in AccountService.LoginAsync.
        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET: api/notifications?page=1&pageSize=20
        // Returns the current user's notifications — unread first, then newest.
        [HttpGet]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _notificationService.GetNotificationsAsync(
                GetCurrentUserId(), page, pageSize);

            if (!result.Success)
                return BadRequest(result);
                
            return Ok(result.Data);
        }

        // PATCH: api/notifications/{id}/read
        // Mark a single notification as read. Returns 403 if the notification belongs to another user.
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var result = await _notificationService.MarkAsReadAsync(GetCurrentUserId(), id);
            if (!result.Success)
            {
                // Ownership failure → 403; not-found → 404; distinguish by message prefix
                if (result.Error!.Contains("permission"))
                    return StatusCode(403, result);
                if (result.Error.Contains("not found"))
                    return NotFound(result);
                return BadRequest(result);
            }

            return Ok(new { message = "Notification marked as read." });
        }

        // PATCH: api/notifications/read-all
        // Mark all of the current user's notifications as read in one bulk operation.
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var result = await _notificationService.MarkAllAsReadAsync(GetCurrentUserId());
            if (!result.Success)
                return BadRequest(result);

            return Ok(new { message = "All notifications marked as read." });
        }

        // DELETE: api/notifications/{id}
        // Delete a notification. Returns 403 if it belongs to another user.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            var result = await _notificationService.DeleteAsync(GetCurrentUserId(), id);
            if (!result.Success)
            {
                if (result.Error!.Contains("permission"))
                    return StatusCode(403, result);
                if (result.Error.Contains("not found"))
                    return NotFound(result);
                return BadRequest(result);
            }

            return Ok(new { message = "Notification deleted." });
        }
    }
}
