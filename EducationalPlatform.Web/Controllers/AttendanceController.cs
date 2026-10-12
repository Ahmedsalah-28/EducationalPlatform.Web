
using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Admin,Assistant")]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Step 1: Choose a year
        public async Task<IActionResult> Index()
        {
            var query = _context.Years
                .Where(y => y.IsActive)
                .AsQueryable();

            if (User.IsInRole("Assistant"))
            {
                var allowedYearIds = await GetAllowedYearIdsAsync();

                if (allowedYearIds == null)
                    return Forbid();

                query = query.Where(y => allowedYearIds.Contains(y.Id));
            }

            var years = await query
                .OrderBy(y => y.Name)
                .ToListAsync();

            return View(years);
        }

        // Step 2: Choose a group within the selected year
        public async Task<IActionResult> GroupsByYear(int id)
        {
            var year = await _context.Years
                .FirstOrDefaultAsync(y => y.Id == id && y.IsActive);

            if (year == null)
                return NotFound();

            if (!await CanAccessYearAsync(id))
                return Forbid();

            var groups = await _context.Groups
                .Where(g => g.YearId == id && g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();

            ViewBag.YearId = year.Id;
            ViewBag.YearName = year.Name;

            return View(groups);
        }

        // Step 3: Display sessions for the selected group
        public async Task<IActionResult> ClassSessionsByGroup(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Year)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return NotFound();

            if (!await CanAccessYearAsync(group.YearId))
                return Forbid();

            var sessions = await _context.ClassSessions
                .Where(s => s.GroupId == id)
                .Include(s => s.Group)
                    .ThenInclude(g => g.Year)
                .OrderByDescending(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.GroupId = group.Id;
            ViewBag.GroupName = group.Name;
            ViewBag.YearId = group.YearId;
            ViewBag.YearName = group.Year.Name;

            return View(sessions);
        }

        // Step 4: Display students and their attendance
        [HttpGet]
        public async Task<IActionResult> Take(int id)
        {
            var session = await _context.ClassSessions
                .Include(s => s.Group)
                .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            if (!await CanAccessSessionAsync(session))
                return Forbid();

            if (session.Status == "Cancelled")
            {
                TempData["Error"] =
                    "Attendance cannot be recorded for a cancelled session.";

                return RedirectToAction(
                    nameof(ClassSessionsByGroup),
                    new { id = session.GroupId });
            }

            var students = await _context.Enrollments
                .Where(e =>
                    e.GroupId == session.GroupId &&
                    e.Status == "Active")
                .Select(e => e.Student)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            var existingRecords = await _context.AttendanceRecords
                .Where(a => a.ClassSessionId == session.Id)
                .ToDictionaryAsync(a => a.StudentId, a => a.Status);

            var model = new AttendanceSessionViewModel
            {
                SessionId = session.Id,
                YearName = session.Group.Year.Name,
                GroupName = session.Group.Name,
                Date = session.Date,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                SessionStatus = session.Status,

                Students = students.Select(student =>
                    new AttendanceStudentViewModel
                    {
                        StudentId = student.Id,
                        FullName = student.FullName,
                        Status = existingRecords.TryGetValue(
                            student.Id, out var status)
                                ? status
                                : "Absent"
                    }).ToList()
            };

            return View(model);
        }

        // Save or update attendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Take(
            AttendanceSessionViewModel model)
        {
            var session = await _context.ClassSessions
                .Include(s => s.Group)
                .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(s => s.Id == model.SessionId);

            if (session == null)
                return NotFound();

            if (!await CanAccessSessionAsync(session))
                return Forbid();

            if (session.Status == "Cancelled")
            {
                TempData["Error"] =
                    "Attendance cannot be recorded for a cancelled session.";

                return RedirectToAction(
                    nameof(ClassSessionsByGroup),
                    new { id = session.GroupId });
            }

            var validStatuses = new[]
            {
                "Present", "Absent", "Late", "Excused"
            };

            var activeStudentIds = await _context.Enrollments
                .Where(e =>
                    e.GroupId == session.GroupId &&
                    e.Status == "Active")
                .Select(e => e.StudentId)
                .ToListAsync();

            var submittedStudents = (model.Students
                    ?? new List<AttendanceStudentViewModel>())
                .Where(s => activeStudentIds.Contains(s.StudentId))
                .ToList();

            if (submittedStudents.Any(s =>
                !validStatuses.Contains(s.Status)))
            {
                ModelState.AddModelError(
                    "", "One or more attendance statuses are invalid.");
            }

            if (submittedStudents.Select(s => s.StudentId)
                .Distinct().Count() != submittedStudents.Count)
            {
                ModelState.AddModelError(
                    "", "Duplicate student entries were submitted.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Attendance data is invalid. Please review and try again.";

                return RedirectToAction(
                    nameof(Take), new { id = session.Id });
            }

            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var existingRecords = await _context.AttendanceRecords
                .Where(a => a.ClassSessionId == session.Id)
                .ToDictionaryAsync(a => a.StudentId);

            foreach (var student in submittedStudents)
            {
                if (existingRecords.TryGetValue(
                    student.StudentId, out var record))
                {
                    record.Status = student.Status;
                    record.RecordedAt = DateTime.UtcNow;
                    record.RecordedByUserId = userId;
                }
                else
                {
                    _context.AttendanceRecords.Add(
                        new AttendanceRecord
                        {
                            ClassSessionId = session.Id,
                            StudentId = student.StudentId,
                            Status = student.Status,
                            RecordedAt = DateTime.UtcNow,
                            RecordedByUserId = userId
                        });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Attendance saved successfully.";

            return RedirectToAction(
                nameof(Take), new { id = session.Id });
        }

        private async Task<List<int>?> GetAllowedYearIdsAsync()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return null;

            var assistant = await _context.Assistants
                .FirstOrDefaultAsync(a =>
                    a.ApplicationUserId == userId &&
                    a.Status == "Active");

            if (assistant == null)
                return null;

            return await _context.AssistantYears
                .Where(ay => ay.AssistantId == assistant.Id)
                .Select(ay => ay.YearId)
                .ToListAsync();
        }

        private async Task<bool> CanAccessYearAsync(int yearId)
        {
            if (User.IsInRole("Admin"))
                return true;

            var allowedYearIds = await GetAllowedYearIdsAsync();

            return allowedYearIds != null
                && allowedYearIds.Contains(yearId);
        }

        private async Task<bool> CanAccessSessionAsync(
            ClassSession session)
        {
            return await CanAccessYearAsync(session.Group.YearId);
        }
    }
}