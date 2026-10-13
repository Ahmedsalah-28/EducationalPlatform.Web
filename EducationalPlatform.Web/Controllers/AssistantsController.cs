using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Controllers
{
    [Authorize]
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

        // ---------- Assistant dashboard (Assistant role only) ----------
        [Authorize(Roles = "Assistant")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return Forbid();

            var assistant = await _context.Assistants
                .AsNoTracking()
                .Include(a => a.AssistantYears)
                    .ThenInclude(ay => ay.Year)
                .FirstOrDefaultAsync(a => a.ApplicationUserId == userId && a.Status == "Active");

            if (assistant == null) return Forbid();

            var yearIds = assistant.AssistantYears.Select(ay => ay.YearId).ToList();
            var groupIds = await _context.Groups.AsNoTracking()
                .Where(g => yearIds.Contains(g.YearId) && g.IsActive)
                .Select(g => g.Id).ToListAsync();

            var activeEnrollments = _context.Enrollments.AsNoTracking()
                .Where(e => groupIds.Contains(e.GroupId) && e.Status == "Active");

            var now = DateTime.Now;
            var upcomingExamRows = await _context.ExamAssignments.AsNoTracking()
                .Where(a => groupIds.Contains(a.GroupId) &&
                    (a.ExamDate.Date > now.Date || (a.ExamDate.Date == now.Date && a.EndTime >= now.TimeOfDay)))
                .OrderBy(a => a.ExamDate).ThenBy(a => a.StartTime)
                .Select(a => new
                {
                    a.Id, Title = a.Exam.Title, YearName = a.Group.Year.Name,
                    GroupName = a.Group.Name, a.ExamDate, a.StartTime, a.EndTime
                }).Take(5).ToListAsync();

            var upcomingQuizRows = await _context.QuizAssignments.AsNoTracking()
                .Where(a => groupIds.Contains(a.GroupId) && a.EndDateTime >= now)
                .OrderBy(a => a.StartDateTime)
                .Select(a => new
                {
                    a.Id, Title = a.Quiz.Title, YearName = a.Group.Year.Name,
                    GroupName = a.Group.Name, a.StartDateTime, a.EndDateTime
                }).Take(5).ToListAsync();

            var upcoming = upcomingExamRows.Select(a => new AssistantUpcomingItem
                {
                    Kind = "Exam", Title = a.Title, YearName = a.YearName,
                    GroupName = a.GroupName, Date = a.ExamDate,
                    Time = a.StartTime.ToString(@"hh\:mm") + " - " + a.EndTime.ToString(@"hh\:mm"),
                    Controller = "Exams", AssignmentId = a.Id
                })
                .Concat(upcomingQuizRows.Select(a => new AssistantUpcomingItem
                {
                    Kind = "Quiz", Title = a.Title, YearName = a.YearName,
                    GroupName = a.GroupName, Date = a.StartDateTime,
                    Time = a.StartDateTime.ToString("hh:mm tt") + " - " + a.EndDateTime.ToString("hh:mm tt"),
                    Controller = "Quizzes", AssignmentId = a.Id
                }))
                .OrderBy(x => x.Date).Take(8).ToList();

            var examAssignments = _context.ExamAssignments.AsNoTracking().Where(a => groupIds.Contains(a.GroupId));
            var quizAssignments = _context.QuizAssignments.AsNoTracking().Where(a => groupIds.Contains(a.GroupId));
            var expectedExamResults = await examAssignments.SumAsync(a => a.Group.Enrollments.Count(e => e.Status == "Active"));
            var recordedExamResults = await examAssignments.SumAsync(a => a.ExamResults.Select(r => r.StudentId).Distinct().Count());
            var expectedQuizAttempts = await quizAssignments.SumAsync(a => a.Group.Enrollments.Count(e => e.Status == "Active"));
            var submittedQuizAttempts = await quizAssignments.SumAsync(a => a.QuizAttempts.Count(q => q.SubmittedAt != null));

            var vm = new AssistantDashboardViewModel
            {
                AssistantName = assistant.FullName,
                TotalYears = assistant.AssistantYears.Count,
                TotalGroups = groupIds.Count,
                TotalStudents = await activeEnrollments.Select(e => e.StudentId).Distinct().CountAsync(),
                TotalTrainings = await _context.TrainingAssignments.AsNoTracking().CountAsync(a => groupIds.Contains(a.GroupId)),
                TotalExams = await examAssignments.CountAsync(),
                TotalQuizzes = await quizAssignments.CountAsync(),
                PendingExamResults = Math.Max(0, expectedExamResults - recordedExamResults),
                PendingQuizAttempts = Math.Max(0, expectedQuizAttempts - submittedQuizAttempts),
                UpcomingItems = upcoming
            };

            return View(vm);
        }

        // ---------- List ----------
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var model = new AssistantViewModel { Status = "Active" };
            await LoadYearsAsync(model);
            return View(model);
        }

        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
