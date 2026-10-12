
using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegistrationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadGroupsAsync();

            return View(new RegistrationRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            RegistrationRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadGroupsAsync(model.GroupId);
                return View(model);
            }

            var existingUser = await _userManager
                .FindByEmailAsync(model.Email.Trim());

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email is already registered. Please use another email.");

                await LoadGroupsAsync(model.GroupId);
                return View(model);
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
                    "Please select a valid active group.");

                await LoadGroupsAsync(model.GroupId);
                return View(model);
            }

            var request = new RegistrationRequest
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone?.Trim(),
                RequestedGroupId = model.GroupId,
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };

            _context.RegistrationRequests.Add(request);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Success));
        }

        [HttpGet]
        public IActionResult Success()
        {
            return View();
        }

        private async Task LoadGroupsAsync(int? selectedGroupId = null)
        {
            var groups = await _context.Groups
                .Include(g => g.Year)
                .Where(g => g.IsActive && g.Year.IsActive)
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