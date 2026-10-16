using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.Services;
using EducationalPlatform.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Student")]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<ChatHub> _hub;

        public NotificationsController(ApplicationDbContext db, IHubContext<ChatHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // Full list page (latest 100).
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var items = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(100)
                .ToListAsync();

            return View(items);
        }

        // Unread count + latest items for the bell dropdown.
        [HttpGet]
        public async Task<IActionResult> Summary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var unread = await _db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            var rows = await _db.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(8)
                .ToListAsync();

            // CreatedAt is stored as UTC; mark it so the browser converts it to local time.
            var items = rows.Select(n => new
            {
                id = n.Id,
                type = n.Type,
                title = n.Title,
                message = n.Message,
                isRead = n.IsRead,
                createdAt = DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc)
            });

            return Json(new { unreadCount = unread, items });
        }

        // Marks one notification as read and opens the page it is about.
        [HttpGet]
        public async Task<IActionResult> Open(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var notification = await _db.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                return NotFound();

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _db.SaveChangesAsync();
                await PushUnreadCountAsync(userId);
            }

            return LocalRedirect(NotificationTypes.UrlFor(notification.Type));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Forbid();

            await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            await PushUnreadCountAsync(userId);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { unreadCount = 0 });

            return RedirectToAction(nameof(Index));
        }

        // Keeps the badge in the student's other open tabs in sync.
        private async Task PushUnreadCountAsync(string userId)
        {
            var count = await _db.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            await _hub.Clients
                .Group($"user_{userId}")
                .SendAsync("NotificationCountChanged", new { unreadCount = count });
        }
    }
}
