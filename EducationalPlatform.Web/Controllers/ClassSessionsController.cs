
using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClassSessionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClassSessionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ClassSessions
        // Display all class sessions in the system.
        public async Task<IActionResult> Index()
        {
            var sessions = await _context.ClassSessions
                .Include(s => s.Group)
                    .ThenInclude(g => g.Year)
                .OrderBy(s => s.Group.Year.Name)
                .ThenBy(s => s.Group.Name)
                .ThenByDescending(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            return View(sessions);
        }

        // GET: /ClassSessions/Browse
        // Display academic years.
        public async Task<IActionResult> Browse()
        {
            var years = await _context.Years
                .Where(y => y.IsActive)
                .OrderBy(y => y.Name)
                .ToListAsync();

            return View(years);
        }

        // GET: /ClassSessions/GroupsByYear/1
        // Display groups belonging to the selected year.
        public async Task<IActionResult> GroupsByYear(int id)
        {
            var year = await _context.Years
                .FirstOrDefaultAsync(y => y.Id == id);

            if (year == null)
                return NotFound();

            var groups = await _context.Groups
                .Where(g => g.YearId == id && g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();

            ViewBag.YearId = year.Id;
            ViewBag.YearName = year.Name;

            return View(groups);
        }

        // GET: /ClassSessions/ByYear/1
        // Display all sessions belonging to the selected year.
        public async Task<IActionResult> ByYear(int id)
        {
            var year = await _context.Years
                .FirstOrDefaultAsync(y => y.Id == id);

            if (year == null)
                return NotFound();

            var sessions = await _context.ClassSessions
                .Include(s => s.Group)
                    .ThenInclude(g => g.Year)
                .Where(s => s.Group.YearId == id)
                .OrderBy(s => s.Group.Name)
                .ThenByDescending(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.SelectedYearId = year.Id;
            ViewBag.SelectedYearName = year.Name;

            return View(sessions);
        }

        // GET: /ClassSessions/ByGroup/1
        // Display all sessions belonging to the selected group.
        public async Task<IActionResult> ByGroup(int id)
        {
            var group = await _context.Groups
                .Include(g => g.Year)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
                return NotFound();

            var sessions = await _context.ClassSessions
                .Include(s => s.Group)
                    .ThenInclude(g => g.Year)
                .Where(s => s.GroupId == id)
                .OrderByDescending(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.GroupId = group.Id;
            ViewBag.GroupName = group.Name;
            ViewBag.YearId = group.YearId;
            ViewBag.YearName = group.Year.Name;

            return View(sessions);
        }

        // GET: /ClassSessions/Create
        // Optional yearId and groupId preserve the current browsing context.
        public async Task<IActionResult> Create(int? yearId, int? groupId)
        {
            if (groupId.HasValue)
            {
                var group = await _context.Groups
                    .Include(g => g.Year)
                    .FirstOrDefaultAsync(g =>
                        g.Id == groupId.Value &&
                        g.IsActive &&
                        g.Year.IsActive);

                if (group == null)
                    return NotFound();

                yearId = group.YearId;
            }

            if (yearId.HasValue)
            {
                var yearExists = await _context.Years
                    .AnyAsync(y =>
                        y.Id == yearId.Value &&
                        y.IsActive);

                if (!yearExists)
                    return NotFound();
            }

            ViewBag.SelectedYearId = yearId;
            ViewBag.SelectedGroupId = groupId;

            await LoadGroupsAsync(
                selectedGroupId: groupId,
                yearId: yearId,
                groupId: groupId);

            return View(new ClassSessionViewModel
            {
                Date = DateTime.Today,
                StartTime = new TimeSpan(16, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                GroupId = groupId ?? 0
            });
        }

        // POST: /ClassSessions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ClassSessionViewModel model,
            int? returnYearId,
            int? returnGroupId)
        {
            ViewBag.SelectedYearId = returnYearId;
            ViewBag.SelectedGroupId = returnGroupId;

            // Validate start and end times.
            ValidateSession(model);

            // Ensure the selected group and its year are active.
            var group = await _context.Groups
                .Include(g => g.Year)
                .FirstOrDefaultAsync(g =>
                    g.Id == model.GroupId &&
                    g.IsActive &&
                    g.Year.IsActive);

            if (group == null)
            {
                ModelState.AddModelError(
                    nameof(model.GroupId),
                    "Please select an active group.");
            }
            else
            {
                // When creating from a group page,
                // the session must belong to that group.
                if (returnGroupId.HasValue &&
                    model.GroupId != returnGroupId.Value)
                {
                    ModelState.AddModelError(
                        nameof(model.GroupId),
                        "Please keep the selected group.");
                }

                // When creating from a year page,
                // the group must belong to that year.
                if (returnYearId.HasValue &&
                    group.YearId != returnYearId.Value)
                {
                    ModelState.AddModelError(
                        nameof(model.GroupId),
                        "Please select a group belonging to the selected year.");
                }
            }

            // Prevent duplicate sessions with the same group,
            // date, start time, and end time.
            if (ModelState.IsValid)
            {
                var sessionDate = model.Date.Date;

                var duplicateExists = await _context.ClassSessions
                    .AnyAsync(s =>
                        s.GroupId == model.GroupId &&
                        s.Date >= sessionDate &&
                        s.Date < sessionDate.AddDays(1) &&
                        s.StartTime == model.StartTime &&
                        s.EndTime == model.EndTime);

                if (duplicateExists)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "A session with the same group, date, and time already exists.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadGroupsAsync(
                    selectedGroupId: model.GroupId,
                    yearId: returnYearId,
                    groupId: returnGroupId);

                return View(model);
            }

            var session = new ClassSession
            {
                GroupId = model.GroupId,
                Date = model.Date.Date,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Status = "Scheduled"
            };

            _context.ClassSessions.Add(session);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Class session created successfully.";

            return RedirectAfterAction(
                returnYearId,
                returnGroupId);
        }

        // GET: /ClassSessions/Edit/5
        public async Task<IActionResult> Edit(
            int? id,
            int? yearId,
            int? groupId)
        {
            if (id == null)
                return NotFound();

            var session = await _context.ClassSessions
                .FirstOrDefaultAsync(s => s.Id == id.Value);

            if (session == null)
                return NotFound();

            if (session.Status == "Cancelled")
            {
                TempData["Error"] =
                    "Cancelled sessions cannot be edited.";

                return RedirectAfterAction(yearId, groupId);
            }

            // If no browsing context was provided,
            // return to the session's own year.
            if (!yearId.HasValue && !groupId.HasValue)
            {
                var sessionGroup = await _context.Groups
                    .FirstOrDefaultAsync(g => g.Id == session.GroupId);

                if (sessionGroup != null)
                    yearId = sessionGroup.YearId;
            }

            ViewBag.SelectedYearId = yearId;
            ViewBag.SelectedGroupId = groupId;

            var model = new ClassSessionViewModel
            {
                Id = session.Id,
                GroupId = session.GroupId,
                Date = session.Date,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Status = session.Status
            };

            await LoadGroupsAsync(
                selectedGroupId: model.GroupId,
                yearId: yearId,
                groupId: groupId);

            return View(model);
        }

        // POST: /ClassSessions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ClassSessionViewModel model,
            int? returnYearId,
            int? returnGroupId)
        {
            if (id != model.Id)
                return NotFound();

            ViewBag.SelectedYearId = returnYearId;
            ViewBag.SelectedGroupId = returnGroupId;

            ValidateSession(model);

            var session = await _context.ClassSessions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            if (session.Status == "Cancelled")
            {
                TempData["Error"] =
                    "Cancelled sessions cannot be edited.";

                return RedirectAfterAction(
                    returnYearId,
                    returnGroupId);
            }

            var group = await _context.Groups
                .Include(g => g.Year)
                .FirstOrDefaultAsync(g =>
                    g.Id == model.GroupId &&
                    g.IsActive &&
                    g.Year.IsActive);

            if (group == null)
            {
                ModelState.AddModelError(
                    nameof(model.GroupId),
                    "Please select an active group.");
            }
            else
            {
                // Preserve the selected group context.
                if (returnGroupId.HasValue &&
                    model.GroupId != returnGroupId.Value)
                {
                    ModelState.AddModelError(
                        nameof(model.GroupId),
                        "Please keep the selected group.");
                }

                // Preserve the selected year context.
                if (returnYearId.HasValue &&
                    group.YearId != returnYearId.Value)
                {
                    ModelState.AddModelError(
                        nameof(model.GroupId),
                        "Please select a group belonging to the selected year.");
                }
            }

            if (ModelState.IsValid)
            {
                var sessionDate = model.Date.Date;

                var duplicateExists = await _context.ClassSessions
                    .AnyAsync(s =>
                        s.Id != id &&
                        s.GroupId == model.GroupId &&
                        s.Date >= sessionDate &&
                        s.Date < sessionDate.AddDays(1) &&
                        s.StartTime == model.StartTime &&
                        s.EndTime == model.EndTime);

                if (duplicateExists)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Another session already exists with the same group, date, and time.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadGroupsAsync(
                    selectedGroupId: model.GroupId,
                    yearId: returnYearId,
                    groupId: returnGroupId);

                return View(model);
            }

            session.GroupId = model.GroupId;
            session.Date = model.Date.Date;
            session.StartTime = model.StartTime;
            session.EndTime = model.EndTime;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Class session updated successfully.";

            return RedirectAfterAction(
                returnYearId,
                returnGroupId);
        }

        // POST: /ClassSessions/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(
            int id,
            int? yearId,
            int? groupId)
        {
            var session = await _context.ClassSessions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            if (session.Status == "Completed")
            {
                TempData["Error"] =
                    "A completed session cannot be cancelled.";

                return RedirectAfterAction(yearId, groupId);
            }

            if (session.Status == "Cancelled")
            {
                TempData["Error"] =
                    "This session is already cancelled.";

                return RedirectAfterAction(yearId, groupId);
            }

            session.Status = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Class session cancelled.";

            return RedirectAfterAction(yearId, groupId);
        }

        // POST: /ClassSessions/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(
            int id,
            int? yearId,
            int? groupId)
        {
            var session = await _context.ClassSessions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            if (session.Status == "Cancelled")
            {
                TempData["Error"] =
                    "A cancelled session cannot be completed.";

                return RedirectAfterAction(yearId, groupId);
            }

            if (session.Status == "Completed")
            {
                TempData["Error"] =
                    "This session is already completed.";

                return RedirectAfterAction(yearId, groupId);
            }

            session.Status = "Completed";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Session marked as completed.";

            return RedirectAfterAction(yearId, groupId);
        }

        // Redirect to the page the admin was browsing.
        private IActionResult RedirectAfterAction(
            int? yearId,
            int? groupId)
        {
            if (groupId.HasValue)
            {
                return RedirectToAction(
                    nameof(ByGroup),
                    new { id = groupId.Value });
            }

            if (yearId.HasValue)
            {
                return RedirectToAction(
                    nameof(ByYear),
                    new { id = yearId.Value });
            }

            return RedirectToAction(nameof(Index));
        }

        // Validate session time range.
        private void ValidateSession(ClassSessionViewModel model)
        {
            if (model.StartTime >= model.EndTime)
            {
                ModelState.AddModelError(
                    nameof(model.EndTime),
                    "End time must be later than start time.");
            }
        }

        // Load active groups, optionally filtered by year or group.
        private async Task LoadGroupsAsync(
            int? selectedGroupId = null,
            int? yearId = null,
            int? groupId = null)
        {
            var groupsQuery = _context.Groups
                .Include(g => g.Year)
                .Where(g => g.IsActive && g.Year.IsActive);

            if (yearId.HasValue)
            {
                groupsQuery = groupsQuery
                    .Where(g => g.YearId == yearId.Value);
            }

            if (groupId.HasValue)
            {
                groupsQuery = groupsQuery
                    .Where(g => g.Id == groupId.Value);
            }

            var groups = await groupsQuery
                .OrderBy(g => g.Year.Name)
                .ThenBy(g => g.Name)
                .Select(g => new
                {
                    g.Id,
                    DisplayName = g.Year.Name + " - " + g.Name
                })
                .ToListAsync();

            ViewBag.Groups = new SelectList(
                groups,
                "Id",
                "DisplayName",
                selectedGroupId);
        }
    }
}

