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
    public class GroupSchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GroupSchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /GroupSchedules
        // Display all schedules
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.GroupSchedules
                .Include(s => s.Group)
                    .ThenInclude(g => g.Year)
                .OrderBy(s => s.Group.Year.Name)
                .ThenBy(s => s.Group.Name)
                .ThenBy(s => s.DayOfWeek)
                .ToListAsync();

            return View(schedules);
        }

        // GET: /GroupSchedules/Browse
        // Display academic years
        public async Task<IActionResult> Browse()
        {
            var years = await _context.Years
                .OrderBy(y => y.Name)
                .ToListAsync();

            return View(years);
        }

        // GET: /GroupSchedules/ByYear/1
        // Display schedules for one academic year
        public async Task<IActionResult> ByYear(int id)
        {
            var year = await _context.Years
                .FirstOrDefaultAsync(y => y.Id == id);

            if (year == null)
            {
                return NotFound();
            }

            var schedules = await _context.GroupSchedules
                .Include(s => s.Group)
                    .ThenInclude(g => g.Year)
                .Where(s => s.Group.YearId == id)
                .OrderBy(s => s.Group.Name)
                .ThenBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.SelectedYearId = year.Id;
            ViewBag.SelectedYearName = year.Name;

            return View(schedules);
        }

        // GET: /GroupSchedules/Create
        public async Task<IActionResult> Create(int? yearId)
        {
            if (yearId.HasValue)
            {
                var yearExists = await _context.Years
                    .AnyAsync(y => y.Id == yearId.Value);

                if (!yearExists)
                {
                    return NotFound();
                }
            }

            ViewBag.SelectedYearId = yearId;

            await LoadGroupsAsync(yearId: yearId);

            return View(new GroupScheduleViewModel());
        }

        // POST: /GroupSchedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            GroupScheduleViewModel model,
            int? returnYearId)
        {
            ViewBag.SelectedYearId = returnYearId;

            ValidateSchedule(model);

            var groupExists = await _context.Groups
                .AnyAsync(g =>
                    g.Id == model.GroupId &&
                    g.IsActive &&
                    g.Year.IsActive);

            if (!groupExists)
            {
                ModelState.AddModelError(
                    nameof(model.GroupId),
                    "Please select an active group.");
            }
            else if (returnYearId.HasValue)
            {
                var groupBelongsToYear = await _context.Groups
                    .AnyAsync(g =>
                        g.Id == model.GroupId &&
                        g.YearId == returnYearId.Value);

                if (!groupBelongsToYear)
                {
                    ModelState.AddModelError(
                        nameof(model.GroupId),
                        "Please select a group belonging to the selected year.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadGroupsAsync(
                    selectedGroupId: model.GroupId,
                    yearId: returnYearId);

                return View(model);
            }

            var schedule = new GroupSchedule
            {
                GroupId = model.GroupId,
                DayOfWeek = model.DayOfWeek,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                IsActive = model.IsActive
            };

            _context.GroupSchedules.Add(schedule);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule created successfully.";

            if (returnYearId.HasValue)
            {
                return RedirectToAction(
                    nameof(ByYear),
                    new { id = returnYearId.Value });
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /GroupSchedules/Edit/5
        public async Task<IActionResult> Edit(
            int? id,
            int? yearId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.GroupSchedules
                .FirstOrDefaultAsync(s => s.Id == id.Value);

            if (schedule == null)
            {
                return NotFound();
            }

            var model = new GroupScheduleViewModel
            {
                Id = schedule.Id,
                GroupId = schedule.GroupId,
                DayOfWeek = schedule.DayOfWeek,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                IsActive = schedule.IsActive
            };

            ViewBag.SelectedYearId = yearId;

            await LoadGroupsAsync(
                selectedGroupId: model.GroupId);

            return View(model);
        }

        // POST: /GroupSchedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            GroupScheduleViewModel model,
            int? returnYearId)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            ViewBag.SelectedYearId = returnYearId;

            ValidateSchedule(model);

            var schedule = await _context.GroupSchedules
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            var groupExists = await _context.Groups
                .AnyAsync(g =>
                    g.Id == model.GroupId &&
                    g.IsActive &&
                    g.Year.IsActive);

            if (!groupExists)
            {
                ModelState.AddModelError(
                    nameof(model.GroupId),
                    "Please select an active group.");
            }

            if (!ModelState.IsValid)
            {
                await LoadGroupsAsync(
                    selectedGroupId: model.GroupId);

                return View(model);
            }

            schedule.GroupId = model.GroupId;
            schedule.DayOfWeek = model.DayOfWeek;
            schedule.StartTime = model.StartTime;
            schedule.EndTime = model.EndTime;
            schedule.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule updated successfully.";

            if (returnYearId.HasValue)
            {
                return RedirectToAction(
                    nameof(ByYear),
                    new { id = returnYearId.Value });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /GroupSchedules/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(
            int id,
            int? yearId)
        {
            var schedule = await _context.GroupSchedules
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null)
            {
                return NotFound();
            }

            schedule.IsActive = false;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Schedule deactivated successfully.";

            if (yearId.HasValue)
            {
                return RedirectToAction(
                    nameof(ByYear),
                    new { id = yearId.Value });
            }

            return RedirectToAction(nameof(Index));
        }

        private void ValidateSchedule(GroupScheduleViewModel model)
        {
            if (model.StartTime >= model.EndTime)
            {
                ModelState.AddModelError(
                    nameof(model.EndTime),
                    "End time must be later than start time.");
            }
        }

        private async Task LoadGroupsAsync(
            int? selectedGroupId = null,
            int? yearId = null)
        {
            var groupsQuery = _context.Groups
                .Include(g => g.Year)
                .Where(g => g.IsActive && g.Year.IsActive);

            if (yearId.HasValue)
            {
                groupsQuery = groupsQuery
                    .Where(g => g.YearId == yearId.Value);
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
