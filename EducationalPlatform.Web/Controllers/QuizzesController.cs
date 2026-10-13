using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Admin,Assistant")]
    public class QuizzesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public QuizzesController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var allowed = await GetAllowedYearIdsAsync();
            var query = _context.Quizzes.AsNoTracking().AsQueryable();
            if (allowed != null) query = query.Where(q => q.QuizAssignments.Any(a => allowed.Contains(a.Group.YearId)) || q.CreatedByUserId == User.FindFirstValue(ClaimTypes.NameIdentifier));
            var items = await query.OrderByDescending(q => q.CreatedAt).Select(q => new { Quiz = q, Count = q.QuizAssignments.Count }).ToListAsync();
            ViewBag.AssignmentCounts = items.ToDictionary(x => x.Quiz.Id, x => x.Count);
            return View(items.Select(x => x.Quiz).ToList());
        }

        public IActionResult Create() => View(new QuizFormViewModel());
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuizFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); if (string.IsNullOrWhiteSpace(userId)) return Forbid();
            _context.Quizzes.Add(new Quiz { Title = model.Title.Trim(), Description = model.Description?.Trim(), MaximumGrade = model.MaximumGrade, DurationMinutes = model.DurationMinutes, CreatedByUserId = userId });
            await _context.SaveChangesAsync(); TempData["Success"] = "Quiz created successfully."; return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound(); var quiz = await _context.Quizzes.Include(q => q.QuizAssignments).FirstOrDefaultAsync(q => q.Id == id);
            if (quiz == null || !await CanManageQuizAsync(quiz)) return quiz == null ? NotFound() : Forbid();
            ViewBag.Locked = quiz.QuizAssignments.Any();
            return View(new QuizFormViewModel { Id = quiz.Id, Title = quiz.Title, Description = quiz.Description, MaximumGrade = quiz.MaximumGrade, DurationMinutes = quiz.DurationMinutes });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QuizFormViewModel model)
        {
            if (id != model.Id) return NotFound(); var quiz = await _context.Quizzes.Include(q => q.QuizAssignments).FirstOrDefaultAsync(q => q.Id == id); if (quiz == null) return NotFound(); if (!await CanManageQuizAsync(quiz)) return Forbid();
            var locked = quiz.QuizAssignments.Any(); ViewBag.Locked = locked;
            if (locked) { model.MaximumGrade = quiz.MaximumGrade; model.DurationMinutes = quiz.DurationMinutes; ModelState.Remove(nameof(model.MaximumGrade)); ModelState.Remove(nameof(model.DurationMinutes)); }
            if (!ModelState.IsValid) return View(model);
            quiz.Title = model.Title.Trim(); quiz.Description = model.Description?.Trim(); if (!locked) { quiz.MaximumGrade = model.MaximumGrade; quiz.DurationMinutes = model.DurationMinutes; }
            await _context.SaveChangesAsync(); TempData["Success"] = "Quiz updated successfully."; return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Assignments(int? yearId, int? groupId)
        {
            var allowed = await GetAllowedYearIdsAsync();
            (yearId, groupId) = await LoadYearGroupFilterAsync(allowed, yearId, groupId);
            var query = _context.QuizAssignments.AsNoTracking().AsQueryable();
            if (allowed != null) query = query.Where(a => allowed.Contains(a.Group.YearId));
            if (groupId.HasValue) query = query.Where(a => a.GroupId == groupId.Value);
            else if (yearId.HasValue) query = query.Where(a => a.Group.YearId == yearId.Value);
            var items = await query.OrderBy(a => a.StartDateTime).Select(a => new QuizAssignmentListItem { Id = a.Id, QuizTitle = a.Quiz.Title, YearName = a.Group.Year.Name, GroupName = a.Group.Name, StartDateTime = a.StartDateTime, EndDateTime = a.EndDateTime, MaximumGrade = a.Quiz.MaximumGrade, SubmittedCount = a.QuizAttempts.Count(t => t.SubmittedAt != null), StudentCount = a.Group.Enrollments.Count(e => e.Status == "Active") }).ToListAsync(); return View(items);
        }
        public async Task<IActionResult> Assign(int? id)
        {
            if (id == null) return NotFound(); var quiz = await _context.Quizzes.FindAsync(id); if (quiz == null || !await CanManageQuizAsync(quiz)) return quiz == null ? NotFound() : Forbid();
            var model = new AssignQuizViewModel { QuizId = quiz.Id, QuizTitle = quiz.Title, StartDateTime = DateTime.Now.AddHours(1), EndDateTime = DateTime.Now.AddHours(2) }; await LoadListsAsync(model);
            if (!model.Years.Any()) { TempData["Error"] = "No active assigned years are available."; return RedirectToAction(nameof(Index)); } return View(model);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignQuizViewModel model)
        {
            var quiz = await _context.Quizzes.FindAsync(model.QuizId); if (quiz == null || !await CanManageQuizAsync(quiz)) return quiz == null ? NotFound() : Forbid(); model.QuizTitle = quiz.Title;
            var allowed = await GetAllowedYearIdsAsync(); if (!CanAccessYear(allowed, model.YearId)) return Forbid();
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == model.GroupId && g.YearId == model.YearId && g.IsActive); if (group == null) ModelState.AddModelError(nameof(model.GroupId), "Choose an active group in the selected year.");
            if (model.EndDateTime <= model.StartDateTime) ModelState.AddModelError(nameof(model.EndDateTime), "End date/time must be after start date/time.");
            if (group != null && await _context.QuizAssignments.AnyAsync(a => a.QuizId == quiz.Id && a.GroupId == group.Id && model.StartDateTime < a.EndDateTime && model.EndDateTime > a.StartDateTime)) ModelState.AddModelError("", "This quiz has an overlapping assignment for this group.");
            if (!ModelState.IsValid) { await LoadListsAsync(model); return View(model); }
            _context.QuizAssignments.Add(new QuizAssignment { QuizId = quiz.Id, GroupId = group!.Id, StartDateTime = model.StartDateTime, EndDateTime = model.EndDateTime }); await _context.SaveChangesAsync(); TempData["Success"] = "Quiz assigned successfully."; return RedirectToAction(nameof(Assignments));
        }
        [HttpGet]
        public async Task<IActionResult> GetGroupsByYear(int yearId)
        {
            if (!CanAccessYear(await GetAllowedYearIdsAsync(), yearId)) return Forbid(); var groups = await _context.Groups.Where(g => g.YearId == yearId && g.IsActive).OrderBy(g => g.Name).Select(g => new { id = g.Id, name = g.Name }).ToListAsync(); return Json(groups);
        }
        public async Task<IActionResult> Attempts(int? id)
        {
            if (id == null) return NotFound(); var assignment = await LoadAssignmentAsync(id.Value); if (assignment == null) return NotFound(); if (!CanAccessYear(await GetAllowedYearIdsAsync(), assignment.Group.YearId)) return Forbid();
            var attempts = await _context.QuizAttempts.Where(a => a.QuizAssignmentId == assignment.Id).Include(a => a.Student).OrderBy(a => a.Student.FullName).ToListAsync();
            return View(new GradeQuizViewModel { AssignmentId = assignment.Id, QuizTitle = assignment.Quiz.Title, YearName = assignment.Group.Year.Name, GroupName = assignment.Group.Name, MaximumGrade = assignment.Quiz.MaximumGrade, Students = attempts.Select(a => new QuizGradeStudent { AttemptId = a.Id, StudentId = a.StudentId, FullName = a.Student.FullName, Score = a.Score, StartedAt = a.StartedAt, SubmittedAt = a.SubmittedAt }).ToList() });
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Attempts(GradeQuizViewModel model)
        {
            var assignment = await LoadAssignmentAsync(model.AssignmentId); if (assignment == null) return NotFound(); if (!CanAccessYear(await GetAllowedYearIdsAsync(), assignment.Group.YearId)) return Forbid();
            var allowedAttemptIds = await _context.QuizAttempts.Where(a => a.QuizAssignmentId == assignment.Id).Select(a => a.Id).ToListAsync();
            foreach (var item in model.Students ?? new()) if (allowedAttemptIds.Contains(item.AttemptId) && (item.Score < 0 || item.Score > assignment.Quiz.MaximumGrade)) ModelState.AddModelError("", $"Scores must be between 0 and {assignment.Quiz.MaximumGrade:0.##}.");
            if (!ModelState.IsValid) return await Attempts(assignment.Id);
            var scores = (model.Students ?? new()).Where(s => allowedAttemptIds.Contains(s.AttemptId)).GroupBy(s => s.AttemptId).Select(g => g.First()).ToDictionary(s => s.AttemptId, s => s.Score);
            var attempts = await _context.QuizAttempts.Where(a => scores.Keys.Contains(a.Id)).ToListAsync(); foreach (var attempt in attempts) { attempt.Score = scores[attempt.Id]; attempt.MaxScore = assignment.Quiz.MaximumGrade; }
            await _context.SaveChangesAsync(); TempData["Success"] = "Quiz scores saved."; return RedirectToAction(nameof(Assignments));
        }

        private async Task<List<int>?> GetAllowedYearIdsAsync()
        {
            if (User.IsInRole("Admin")) return null; var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); if (string.IsNullOrWhiteSpace(userId)) return new List<int>(); var assistant = await _context.Assistants.FirstOrDefaultAsync(a => a.ApplicationUserId == userId && a.Status == "Active"); return assistant == null ? new List<int>() : await _context.AssistantYears.Where(a => a.AssistantId == assistant.Id).Select(a => a.YearId).ToListAsync();
        }
        // Fills the Year / Group filter dropdowns (ViewBag) and returns the cleaned filter values.
        // Assistants only see their assigned years; a group that is not in the chosen year is ignored.
        private async Task<(int? YearId, int? GroupId)> LoadYearGroupFilterAsync(
            List<int>? allowed, int? yearId, int? groupId)
        {
            var yearsQuery = _context.Years.AsQueryable();
            if (allowed != null)
                yearsQuery = yearsQuery.Where(y => allowed.Contains(y.Id));

            var years = await yearsQuery.OrderBy(y => y.Name).ToListAsync();

            if (!yearId.HasValue || !years.Any(y => y.Id == yearId.Value))
            {
                yearId = null;
                groupId = null;
            }

            if (yearId.HasValue && groupId.HasValue)
            {
                var groupInYear = await _context.Groups
                    .AnyAsync(g => g.Id == groupId.Value && g.YearId == yearId.Value);

                if (!groupInYear)
                    groupId = null;
            }

            ViewBag.Years = new SelectList(years, "Id", "Name", yearId);

            if (yearId.HasValue)
            {
                var groups = await _context.Groups
                    .Where(g => g.YearId == yearId.Value && (g.IsActive || g.Id == groupId))
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                ViewBag.Groups = new SelectList(groups, "Id", "Name", groupId);
            }
            else
            {
                ViewBag.Groups = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            ViewBag.SelectedYearId = yearId;
            ViewBag.SelectedGroupId = groupId;

            return (yearId, groupId);
        }

        private static bool CanAccessYear(List<int>? allowed, int yearId) => allowed == null || allowed.Contains(yearId);
        private async Task<bool> CanManageQuizAsync(Quiz quiz)
        {
            if (User.IsInRole("Admin")) return true; var allowed = await GetAllowedYearIdsAsync(); if (allowed == null) return true; if (quiz.CreatedByUserId == User.FindFirstValue(ClaimTypes.NameIdentifier)) return true; return await _context.QuizAssignments.AnyAsync(a => a.QuizId == quiz.Id && allowed.Contains(a.Group.YearId));
        }
        private async Task LoadListsAsync(AssignQuizViewModel model)
        {
            var allowed = await GetAllowedYearIdsAsync(); var years = _context.Years.Where(y => y.IsActive); if (allowed != null) years = years.Where(y => allowed.Contains(y.Id)); model.Years = await years.OrderBy(y => y.Name).Select(y => new SelectListItem { Value = y.Id.ToString(), Text = y.Name, Selected = y.Id == model.YearId }).ToListAsync(); model.Groups = model.YearId > 0 && CanAccessYear(allowed, model.YearId) ? await _context.Groups.Where(g => g.YearId == model.YearId && g.IsActive).OrderBy(g => g.Name).Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Name, Selected = g.Id == model.GroupId }).ToListAsync() : new();
        }
        private Task<QuizAssignment?> LoadAssignmentAsync(int id) => _context.QuizAssignments.Include(a => a.Quiz).Include(a => a.Group).ThenInclude(g => g.Year).FirstOrDefaultAsync(a => a.Id == id);
    }
}
