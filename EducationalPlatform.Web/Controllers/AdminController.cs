using EducationalPlatform.Data;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var todaySessions = await _context.ClassSessions
                .AsNoTracking()
                .Where(s => s.Date.Date == today && s.Status != "Cancelled")
                .OrderBy(s => s.StartTime)
                .Select(s => new
                {
                    s.Id,
                    GroupName = s.Group.Name,
                    YearName = s.Group.Year.Name,
                    s.StartTime,
                    s.EndTime,
                    s.Status
                })
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalYears = await _context.Years.CountAsync(),
                TotalGroups = await _context.Groups.CountAsync(),
                TotalStudents = await _context.Students.CountAsync(),
                TotalAssistants = await _context.Assistants.CountAsync(),
                TotalSchedules = await _context.GroupSchedules.CountAsync(),
                TotalSessions = await _context.ClassSessions.CountAsync(),
                PendingRequests = await _context.RegistrationRequests
                    .CountAsync(r => r.Status == "Pending"),

                // TimeSpan formatting happens in memory, after the query
                TodaySessions = todaySessions.Select(s => new TodaySessionItem
                {
                    SessionId = s.Id,
                    GroupName = s.GroupName,
                    YearName = s.YearName,
                    Time = $"{DateTime.Today.Add(s.StartTime):hh:mm tt} - {DateTime.Today.Add(s.EndTime):hh:mm tt}",
                    Status = s.Status
                }).ToList()
            };

            return View(vm);
        }
    }
}
