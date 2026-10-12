using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Admin")]
    public class YearsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public YearsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Years
        public async Task<IActionResult> Index()
        {
            var years = await _context.Years
                .OrderBy(y => y.Name)
                .ToListAsync();

            return View(years);
        }

        // GET: /Years/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Years/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(YearViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var year = new Year
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = model.IsActive
            };

            _context.Years.Add(year);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Years/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var year = await _context.Years.FindAsync(id);

            if (year == null)
            {
                return NotFound();
            }

            var model = new YearViewModel
            {
                Id = year.Id,
                Name = year.Name,
                Description = year.Description,
                IsActive = year.IsActive
            };

            return View(model);
        }

        // POST: /Years/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            YearViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var year = await _context.Years.FindAsync(id);

            if (year == null)
            {
                return NotFound();
            }

            year.Name = model.Name;
            year.Description = model.Description;
            year.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Years/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var year = await _context.Years.FindAsync(id);

            if (year == null)
            {
                return NotFound();
            }

            year.IsActive = false;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}