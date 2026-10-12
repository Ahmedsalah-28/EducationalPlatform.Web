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
    public class ExamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ExamsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var allowed = await GetAllowedYearIdsAsync();
            var query = _context.Exams.AsNoTracking().AsQueryable();
            if (allowed != null) query = query.Where(e => e.ExamAssignments.Any(a => allowed.Contains(a.Group.YearId)) || e.CreatedByUserId == User.FindFirstValue(ClaimTypes.NameIdentifier));
            var items = await query.OrderByDescending(e => e.CreatedAt)
                .Select(e => new { Exam = e, Count = e.ExamAssignments.Count })
                .ToListAsync();
            ViewBag.AssignmentCounts = items.ToDictionary(x => x.Exam.Id, x => x.Count);
            return View(items.Select(x => x.Exam).ToList());
        }

        public IActionResult Create() => View(new ExamFormViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Forbid();
            _context.Exams.Add(new Exam { Title = model.Title.Trim(), Description = model.Description?.Trim(), MaximumGrade = model.MaximumGrade, PassingPercentage = model.PassingPercentage, CreatedByUserId = userId });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Exam created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var exam = await _context.Exams.Include(e => e.ExamAssignments).FirstOrDefaultAsync(e => e.Id == id);
            if (exam == null || !await CanManageExamAsync(exam)) return exam == null ? NotFound() : Forbid();
            if (exam.ExamAssignments.Any()) ViewBag.Locked = true;
            return View(new ExamFormViewModel { Id = exam.Id, Title = exam.Title, Description = exam.Description, MaximumGrade = exam.MaximumGrade, PassingPercentage = exam.PassingPercentage });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExamFormViewModel model)
        {
            if (id != model.Id) return NotFound();
            var exam = await _context.Exams.Include(e => e.ExamAssignments).FirstOrDefaultAsync(e => e.Id == id);
            if (exam == null) return NotFound();
            if (!await CanManageExamAsync(exam)) return Forbid();
            var locked = exam.ExamAssignments.Any();
            ViewBag.Locked = locked;
            if (locked) { model.MaximumGrade = exam.MaximumGrade; model.PassingPercentage = exam.PassingPercentage; ModelState.Remove(nameof(model.MaximumGrade)); ModelState.Remove(nameof(model.PassingPercentage)); }
            if (!ModelState.IsValid) return View(model);
            exam.Title = model.Title.Trim(); exam.Description = model.Description?.Trim();
            if (!locked) { exam.MaximumGrade = model.MaximumGrade; exam.PassingPercentage = model.PassingPercentage; }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Exam updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Assignments()
        {
            var allowed = await GetAllowedYearIdsAsync();
            var query = _context.ExamAssignments.AsNoTracking().AsQueryable();
            if (allowed != null) query = query.Where(a => allowed.Contains(a.Group.YearId));
            var items = await query.OrderBy(a => a.ExamDate).ThenBy(a => a.StartTime)
                .Select(a => new ExamAssignmentListItem { Id = a.Id, ExamTitle = a.Exam.Title, YearName = a.Group.Year.Name, GroupName = a.Group.Name, ExamDate = a.ExamDate, StartTime = a.StartTime, EndTime = a.EndTime, MaximumGrade = a.Exam.MaximumGrade, GradedCount = a.ExamResults.Select(r => r.StudentId).Distinct().Count(), StudentCount = a.Group.Enrollments.Count(e => e.Status == "Active") }).ToListAsync();
            return View(items);
        }

        public async Task<IActionResult> Assign(int? id)
        {
            if (id == null) return NotFound();
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null || !await CanManageExamAsync(exam)) return exam == null ? NotFound() : Forbid();
            var model = new AssignExamViewModel { ExamId = exam.Id, ExamTitle = exam.Title, ExamDate = DateTime.Today };
            await LoadListsAsync(model);
            if (!model.Years.Any()) { TempData["Error"] = "No active assigned years are available."; return RedirectToAction(nameof(Index)); }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignExamViewModel model)
        {
            var exam = await _context.Exams.FindAsync(model.ExamId);
            if (exam == null || !await CanManageExamAsync(exam)) return exam == null ? NotFound() : Forbid();
            model.ExamTitle = exam.Title;
            var allowed = await GetAllowedYearIdsAsync();
            if (!CanAccessYear(allowed, model.YearId)) return Forbid();
            var group = await _context.Groups.FirstOrDefaultAsync(g => g.Id == model.GroupId && g.YearId == model.YearId && g.IsActive);
            if (group == null) ModelState.AddModelError(nameof(model.GroupId), "Choose an active group in the selected year.");
            if (model.EndTime <= model.StartTime) ModelState.AddModelError(nameof(model.EndTime), "End time must be after start time.");
            if (group != null && await _context.ExamAssignments.AnyAsync(a => a.ExamId == exam.Id && a.GroupId == group.Id && a.ExamDate.Date == model.ExamDate.Date && model.StartTime < a.EndTime && model.EndTime > a.StartTime)) ModelState.AddModelError("", "This exam has an overlapping assignment for this group on that date.");
            if (!ModelState.IsValid) { await LoadListsAsync(model); return View(model); }
            _context.ExamAssignments.Add(new ExamAssignment { ExamId = exam.Id, GroupId = group!.Id, ExamDate = model.ExamDate.Date, StartTime = model.StartTime, EndTime = model.EndTime });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Exam assigned successfully.";
            return RedirectToAction(nameof(Assignments));
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupsByYear(int yearId)
        {
            if (!CanAccessYear(await GetAllowedYearIdsAsync(), yearId)) return Forbid();
            var groups = await _context.Groups.Where(g => g.YearId == yearId && g.IsActive).OrderBy(g => g.Name).Select(g => new { id = g.Id, name = g.Name }).ToListAsync();
            return Json(groups);
        }

        public async Task<IActionResult> Grade(int? id)
        {
            if (id == null) return NotFound();
            var assignment = await LoadAssignmentAsync(id.Value);
            if (assignment == null) return NotFound();
            if (!CanAccessYear(await GetAllowedYearIdsAsync(), assignment.Group.YearId)) return Forbid();
            var scores = await _context.ExamResults.Where(r => r.ExamAssignmentId == assignment.Id).ToDictionaryAsync(r => r.StudentId, r => (decimal?)r.Score);
            return View(await BuildGradeModelAsync(assignment, scores));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(GradeExamViewModel model)
        {
            var assignment = await LoadAssignmentAsync(model.AssignmentId);
            if (assignment == null) return NotFound();
            if (!CanAccessYear(await GetAllowedYearIdsAsync(), assignment.Group.YearId)) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return Forbid();
            var activeIds = await _context.Enrollments.Where(e => e.GroupId == assignment.GroupId && e.Status == "Active").Select(e => e.StudentId).ToListAsync();
            var submitted = (model.Students ?? new()).Where(s => activeIds.Contains(s.StudentId)).GroupBy(s => s.StudentId).Select(g => g.First()).ToList();
            foreach (var s in submitted) if (s.Score.HasValue && (s.Score < 0 || s.Score > assignment.Exam.MaximumGrade)) ModelState.AddModelError("", $"Scores must be between 0 and {assignment.Exam.MaximumGrade:0.##}.");
            if (!ModelState.IsValid) return View(await BuildGradeModelAsync(assignment, submitted.ToDictionary(s => s.StudentId, s => s.Score)));
            var records = await _context.ExamResults.Where(r => r.ExamAssignmentId == assignment.Id).ToDictionaryAsync(r => r.StudentId);
            foreach (var s in submitted)
            {
                if (!s.Score.HasValue) continue;
                if (records.TryGetValue(s.StudentId, out var result)) { result.Score = s.Score.Value; result.MaxScore = assignment.Exam.MaximumGrade; result.RecordedAt = DateTime.UtcNow; result.RecordedByUserId = userId; }
                else _context.ExamResults.Add(new ExamResult { ExamAssignmentId = assignment.Id, StudentId = s.StudentId, Score = s.Score.Value, MaxScore = assignment.Exam.MaximumGrade, RecordedAt = DateTime.UtcNow, RecordedByUserId = userId });
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Exam results saved.";
            return RedirectToAction(nameof(Assignments));
        }

        private async Task<List<int>?> GetAllowedYearIdsAsync()
        {
            if (User.IsInRole("Admin")) return null;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return new List<int>();
            var assistant = await _context.Assistants.FirstOrDefaultAsync(a => a.ApplicationUserId == userId && a.Status == "Active");
            return assistant == null ? new List<int>() : await _context.AssistantYears.Where(a => a.AssistantId == assistant.Id).Select(a => a.YearId).ToListAsync();
        }
        private static bool CanAccessYear(List<int>? allowed, int yearId) => allowed == null || allowed.Contains(yearId);
        private async Task<bool> CanManageExamAsync(Exam exam)
        {
            if (User.IsInRole("Admin")) return true;
            var allowed = await GetAllowedYearIdsAsync();
            if (allowed == null) return true;
            if (exam.CreatedByUserId == User.FindFirstValue(ClaimTypes.NameIdentifier)) return true;
            return await _context.ExamAssignments.AnyAsync(a => a.ExamId == exam.Id && allowed.Contains(a.Group.YearId));
        }
        private async Task LoadListsAsync(AssignExamViewModel model)
        {
            var allowed = await GetAllowedYearIdsAsync();
            var years = _context.Years.Where(y => y.IsActive);
            if (allowed != null) years = years.Where(y => allowed.Contains(y.Id));
            model.Years = await years.OrderBy(y => y.Name).Select(y => new SelectListItem { Value = y.Id.ToString(), Text = y.Name, Selected = y.Id == model.YearId }).ToListAsync();
            model.Groups = model.YearId > 0 && CanAccessYear(allowed, model.YearId) ? await _context.Groups.Where(g => g.YearId == model.YearId && g.IsActive).OrderBy(g => g.Name).Select(g => new SelectListItem { Value = g.Id.ToString(), Text = g.Name, Selected = g.Id == model.GroupId }).ToListAsync() : new();
        }
        private Task<ExamAssignment?> LoadAssignmentAsync(int id) => _context.ExamAssignments.Include(a => a.Exam).Include(a => a.Group).ThenInclude(g => g.Year).FirstOrDefaultAsync(a => a.Id == id);
        private async Task<GradeExamViewModel> BuildGradeModelAsync(ExamAssignment a, Dictionary<int, decimal?> scores)
        {
            var students = await _context.Enrollments.Where(e => e.GroupId == a.GroupId && e.Status == "Active").Select(e => new { e.StudentId, e.Student.FullName }).OrderBy(s => s.FullName).ToListAsync();
            return new GradeExamViewModel { AssignmentId = a.Id, ExamTitle = a.Exam.Title, YearName = a.Group.Year.Name, GroupName = a.Group.Name, MaximumGrade = a.Exam.MaximumGrade, Students = students.Select(s => new ExamGradeStudent { StudentId = s.StudentId, FullName = s.FullName, Score = scores.TryGetValue(s.StudentId, out var score) ? score : null }).ToList() };
        }
    }
}
