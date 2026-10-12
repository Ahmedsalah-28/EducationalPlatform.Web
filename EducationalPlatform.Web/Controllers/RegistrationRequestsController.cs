
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
    public class RegistrationRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegistrationRequestsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _context.RegistrationRequests
                .Include(r => r.RequestedGroup)
                    .ThenInclude(g => g.Year)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var request = await _context.RegistrationRequests
                .Include(r => r.RequestedGroup)
                    .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            if (request.Status != "Pending")
            {
                TempData["Error"] = "This request has already been reviewed.";
                return RedirectToAction(nameof(Index));
            }

            var model = new ReviewRegistrationRequestViewModel
            {
                RequestId = request.Id,
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                GroupName = $"{request.RequestedGroup.Year.Name} - {request.RequestedGroup.Name}"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(
            ReviewRegistrationRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadRequestDetailsAsync(model);
                return View(model);
            }

            var request = await _context.RegistrationRequests
                .Include(r => r.RequestedGroup)
                    .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);

            if (request == null)
                return NotFound();

            if (request.Status != "Pending")
            {
                TempData["Error"] = "This request has already been reviewed.";
                return RedirectToAction(nameof(Index));
            }

            var group = request.RequestedGroup;

            if (!group.IsActive || !group.Year.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "The requested group or year is no longer active.");

                await LoadRequestDetailsAsync(model);
                return View(model);
            }

            var existingUser = await _userManager
                .FindByEmailAsync(request.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "",
                    "This email already has an account. The request cannot be approved.");

                await LoadRequestDetailsAsync(model);
                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    PhoneNumber = request.Phone,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(
                    user,
                    model.Password);

                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync();

                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }

                    await LoadRequestDetailsAsync(model);
                    return View(model);
                }

                var roleResult = await _userManager.AddToRoleAsync(
                    user,
                    "Student");

                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();

                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }

                    await LoadRequestDetailsAsync(model);
                    return View(model);
                }

                var student = new Student
                {
                    ApplicationUserId = user.Id,
                    FullName = request.FullName,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Students.Add(student);

                var enrollment = new Enrollment
                {
                    Student = student,
                    GroupId = request.RequestedGroupId,
                    JoinedAt = DateTime.Today,
                    Status = "Active"
                };

                _context.Enrollments.Add(enrollment);

                request.Status = "Approved";
                request.ReviewedAt = DateTime.UtcNow;
                request.ReviewedByUserId =
                    _userManager.GetUserId(User);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] =
                    "Request approved and student account created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "An unexpected error occurred. Please try again.");

                await LoadRequestDetailsAsync(model);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.RegistrationRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
                return NotFound();

            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "This request has already been reviewed.";

                return RedirectToAction(nameof(Index));
            }

            request.Status = "Rejected";
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedByUserId =
                _userManager.GetUserId(User);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Registration request rejected.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadRequestDetailsAsync(
            ReviewRegistrationRequestViewModel model)
        {
            var request = await _context.RegistrationRequests
                .Include(r => r.RequestedGroup)
                    .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);

            if (request == null)
                return;

            model.FullName = request.FullName;
            model.Email = request.Email;
            model.Phone = request.Phone;
            model.GroupName =
                $"{request.RequestedGroup.Year.Name} - {request.RequestedGroup.Name}";
        }
    }
}