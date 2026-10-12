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
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GroupsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Groups
        // Display all groups
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups
                .Include(g => g.Year)
                .OrderBy(g => g.Year.Name)
                .ThenBy(g => g.Name)
                .ToListAsync();

            return View(groups);
        }

        // GET: /Groups/Browse
        // Display academic years to choose from
        public async Task<IActionResult> Browse()
        {
            var years = await _context.Years
                .OrderBy(y => y.Name)
                .ToListAsync();

            return View(years);
        }

        // GET: /Groups/ByYear/1
        // Display groups belonging to a selected year
        public async Task<IActionResult> ByYear(int id)
        {
            var year = await _context.Years
                .FirstOrDefaultAsync(y => y.Id == id);

            if (year == null)
            {
                return NotFound();
            }

            var groups = await _context.Groups
                .Include(g => g.Year)
                .Where(g => g.YearId == id)
                .OrderBy(g => g.Name)
                .ToListAsync();

            ViewBag.SelectedYearId = year.Id;
            ViewBag.SelectedYearName = year.Name;

            return View(groups);
        }

        // GET: /Groups/Create
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

            await LoadYearsAsync(yearId);

            var model = new GroupViewModel();

            if (yearId.HasValue)
            {
                model.YearId = yearId.Value;
            }

            return View(model);
        }

        // POST: /Groups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            GroupViewModel model,
            int? returnYearId)
        {
            ViewBag.SelectedYearId = returnYearId;

            if (!ModelState.IsValid)
            {
                await LoadYearsAsync(model.YearId);
                return View(model);
            }

            var yearExists = await _context.Years
                .AnyAsync(y => y.Id == model.YearId);

            if (!yearExists)
            {
                ModelState.AddModelError(
                    nameof(model.YearId),
                    "Selected year does not exist.");

                await LoadYearsAsync(model.YearId);
                return View(model);
            }

            var group = new Group
            {
                YearId = model.YearId,
                Name = model.Name,
                Description = model.Description,
                IsActive = model.IsActive
            };

            _context.Groups.Add(group);

            await _context.SaveChangesAsync();

            // Return to the selected year when applicable
            if (returnYearId.HasValue)
            {
                return RedirectToAction(
                    nameof(ByYear),
                    new { id = returnYearId.Value });
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Groups/Edit/5
        public async Task<IActionResult> Edit(
            int? id,
            int? yearId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id.Value);

            if (group == null)
            {
                return NotFound();
            }

            // If no return year was supplied, use the group's current year
            var returnYearId = yearId ?? group.YearId;

            var model = new GroupViewModel
            {
                Id = group.Id,
                YearId = group.YearId,
                Name = group.Name,
                Description = group.Description,
                IsActive = group.IsActive
            };

            ViewBag.SelectedYearId = returnYearId;

            await LoadYearsAsync(model.YearId);

            return View(model);
        }

        // POST: /Groups/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            GroupViewModel model,
            int? returnYearId)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            ViewBag.SelectedYearId = returnYearId;

            if (!ModelState.IsValid)
            {
                await LoadYearsAsync(model.YearId);
                return View(model);
            }

            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            var yearExists = await _context.Years
                .AnyAsync(y => y.Id == model.YearId);

            if (!yearExists)
            {
                ModelState.AddModelError(
                    nameof(model.YearId),
                    "Selected year does not exist.");

                await LoadYearsAsync(model.YearId);
                return View(model);
            }

            group.YearId = model.YearId;
            group.Name = model.Name;
            group.Description = model.Description;
            group.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            if (returnYearId.HasValue)
            {
                return RedirectToAction(
                    nameof(ByYear),
                    new { id = returnYearId.Value });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Groups/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(
            int id,
            int? yearId)
        {
            var group = await _context.Groups
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null)
            {
                return NotFound();
            }

            group.IsActive = false;

            await _context.SaveChangesAsync();

            if (yearId.HasValue)
            {
                return RedirectToAction(
                    nameof(ByYear),
                    new { id = yearId.Value });
            }

            return RedirectToAction(nameof(Index));
        }

        // Load active years into the dropdown
        private async Task LoadYearsAsync(
            int? selectedYearId = null)
        {
            var years = await _context.Years
                .Where(y => y.IsActive)
                .OrderBy(y => y.Name)
                .ToListAsync();

            ViewBag.Years = new SelectList(
                years,
                "Id",
                "Name",
                selectedYearId);
        }
    }
}
