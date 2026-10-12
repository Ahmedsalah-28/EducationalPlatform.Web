using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AssistantsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssistantsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ---------- List ----------
        public async Task<IActionResult> Index()
        {
            var assistants = await _context.Assistants
                .Include(a => a.ApplicationUser)
                .Include(a => a.AssistantYears)
                    .ThenInclude(ay => ay.Year)
                .OrderBy(a => a.FullName)
                .ToListAsync();

            return View(assistants);
        }

        // ---------- Create ----------
        public async Task<IActionResult> Create()
        {
            var model = new AssistantViewModel { Status = "Active" };
            await LoadYearsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AssistantViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError(nameof(model.Email), "Email is required.");

            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "Password is required.");

            if (model.SelectedYearIds.Count == 0)
                ModelState.AddModelError(nameof(model.SelectedYearIds),
                    "Select at least one year.");

            if (!string.IsNullOrWhiteSpace(model.Email)
                && await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email),
                    "This email is already registered.");
            }

            if (!ModelState.IsValid)
            {
                await LoadYearsAsync(model);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Phone,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password!);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                await LoadYearsAsync(model);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Assistant");

            var assistant = new Assistant
            {
                ApplicationUserId = user.Id,
                FullName = model.FullName,
                Status = model.Status
            };

            _context.Assistants.Add(assistant);
            await _context.SaveChangesAsync();

            await SyncYearsAsync(assistant.Id, model.SelectedYearIds);

            TempData["Success"] = $"Assistant \"{assistant.FullName}\" was created.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Edit ----------
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var assistant = await _context.Assistants
                .Include(a => a.ApplicationUser)
                .Include(a => a.AssistantYears)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assistant == null)
                return NotFound();

            var model = new AssistantViewModel
            {
                Id = assistant.Id,
                FullName = assistant.FullName,
                Email = assistant.ApplicationUser.Email,
                Phone = assistant.ApplicationUser.PhoneNumber,
                Status = assistant.Status,
                SelectedYearIds = assistant.AssistantYears
                    .Select(ay => ay.YearId).ToList()
            };

            await LoadYearsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AssistantViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var assistant = await _context.Assistants
                .Include(a => a.ApplicationUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assistant == null)
                return NotFound();

            if (model.SelectedYearIds.Count == 0)
                ModelState.AddModelError(nameof(model.SelectedYearIds),
                    "Select at least one year.");

            if (!ModelState.IsValid)
            {
                model.Email = assistant.ApplicationUser.Email;
                await LoadYearsAsync(model);
                return View(model);
            }

            assistant.FullName = model.FullName;
            assistant.Status = model.Status;
            assistant.ApplicationUser.PhoneNumber = model.Phone;

            var update = await _userManager.UpdateAsync(assistant.ApplicationUser);

            if (!update.Succeeded)
            {
                foreach (var error in update.Errors)
                    ModelState.AddModelError("", error.Description);

                model.Email = assistant.ApplicationUser.Email;
                await LoadYearsAsync(model);
                return View(model);
            }

            // Optional password reset
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager
                    .GeneratePasswordResetTokenAsync(assistant.ApplicationUser);

                var reset = await _userManager.ResetPasswordAsync(
                    assistant.ApplicationUser, token, model.Password);

                if (!reset.Succeeded)
                {
                    foreach (var error in reset.Errors)
                        ModelState.AddModelError("", error.Description);

                    model.Email = assistant.ApplicationUser.Email;
                    await LoadYearsAsync(model);
                    return View(model);
                }
            }

            await _context.SaveChangesAsync();
            await SyncYearsAsync(assistant.Id, model.SelectedYearIds);

            TempData["Success"] = $"Assistant \"{assistant.FullName}\" was updated.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Activate / Deactivate ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var assistant = await _context.Assistants.FindAsync(id);

            if (assistant == null)
                return NotFound();

            assistant.Status = assistant.Status == "Active" ? "Inactive" : "Active";
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"\"{assistant.FullName}\" is now {assistant.Status.ToLower()}.";

            return RedirectToAction(nameof(Index));
        }

        // ---------- Helpers ----------
        private async Task LoadYearsAsync(AssistantViewModel model)
        {
            // Active years, plus any inactive year this assistant already has
            model.AvailableYears = await _context.Years
                .Where(y => y.IsActive || model.SelectedYearIds.Contains(y.Id))
                .OrderBy(y => y.Name)
                .Select(y => new YearOption { Id = y.Id, Name = y.Name })
                .ToListAsync();
        }

        private async Task SyncYearsAsync(int assistantId, List<int> selectedYearIds)
        {
            var validIds = await _context.Years
                .Where(y => selectedYearIds.Contains(y.Id))
                .Select(y => y.Id)
                .ToListAsync();

            var current = await _context.AssistantYears
                .Where(ay => ay.AssistantId == assistantId)
                .ToListAsync();

            _context.AssistantYears.RemoveRange(
                current.Where(ay => !validIds.Contains(ay.YearId)));

            foreach (var yearId in validIds
                .Where(y => !current.Any(ay => ay.YearId == y)))
            {
                _context.AssistantYears.Add(new AssistantYear
                {
                    AssistantId = assistantId,
                    YearId = yearId
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
