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
    public class TrainingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrainingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // Training definitions
        // =====================================================

        public async Task<IActionResult> Index()
        {
            var trainings = await _context.Trainings
                .Include(t => t.TrainingAssignments)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(trainings);
        }

        public IActionResult Create()
        {
            return View(new TrainingViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TrainingViewModel model)
        {
            if (!IsValidType(model.Type))
                ModelState.AddModelError(nameof(model.Type), "Invalid training type.");

            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            _context.Trainings.Add(new Training
            {
                Title = model.Title.Trim(),
                Description = model.Description,
                Type = model.Type,
                MaximumGrade = model.MaximumGrade,
                CreatedByUserId = userId
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Training created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var training = await _context.Trainings
                .Include(t => t.TrainingAssignments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (training == null)
                return NotFound();

            ViewBag.Locked = training.TrainingAssignments.Any();

            return View(new TrainingViewModel
            {
                Id = training.Id,
                Title = training.Title,
                Description = training.Description,
                Type = training.Type,
                MaximumGrade = training.MaximumGrade
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TrainingViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var training = await _context.Trainings
                .Include(t => t.TrainingAssignments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (training == null)
                return NotFound();

            // Once a training has been assigned, its type and maximum grade
            // are locked so that existing grades stay consistent.
            var locked = training.TrainingAssignments.Any();
            ViewBag.Locked = locked;

            if (locked)
            {
                model.Type = training.Type;
                model.MaximumGrade = training.MaximumGrade;
                ModelState.Remove(nameof(model.Type));
                ModelState.Remove(nameof(model.MaximumGrade));
            }
            else if (!IsValidType(model.Type))
            {
                ModelState.AddModelError(nameof(model.Type), "Invalid training type.");
            }

            if (!ModelState.IsValid)
                return View(model);

            training.Title = model.Title.Trim();
            training.Description = model.Description;

            if (!locked)
            {
                training.Type = model.Type;
                training.MaximumGrade = model.MaximumGrade;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Training updated.";
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // Assignments
        // =====================================================

        // All assignments the current user is allowed to see
        public async Task<IActionResult> Assignments(int? yearId, int? groupId)
        {
            var allowed = await GetAllowedYearIdsAsync();
            (yearId, groupId) = await LoadYearGroupFilterAsync(allowed, yearId, groupId);

            var query = _context.TrainingAssignments.AsQueryable();

            if (allowed != null)
                query = query.Where(a => allowed.Contains(a.Group.YearId));

            if (groupId.HasValue) query = query.Where(a => a.GroupId == groupId.Value);
            else if (yearId.HasValue) query = query.Where(a => a.Group.YearId == yearId.Value);

            var items = await query
                .OrderByDescending(a => a.AssignedDate)
                .ThenByDescending(a => a.Id)
                .Select(a => new TrainingAssignmentListItem
                {
                    Id = a.Id,
                    TrainingTitle = a.Training.Title,
                    TrainingType = a.Training.Type,
                    YearName = a.Group.Year.Name,
                    GroupName = a.Group.Name,
                    AssignedDate = a.AssignedDate,
                    MaxGrade = a.Training.MaximumGrade,
                    GradedCount = a.Training.Type == "InClass"
                        ? a.TrainingRecords.Count
                        : a.TrainingAttempts.Count(t => t.SubmittedAt != null),
                    StudentCount = a.Group.Enrollments
                        .Count(e => e.Status == "Active")
                })
                .ToListAsync();

            return View(items);
        }

        public async Task<IActionResult> Assign(int? id)
        {
            if (id == null)
                return NotFound();

            var training = await _context.Trainings.FindAsync(id);

            if (training == null)
                return NotFound();

            var model = new AssignTrainingViewModel
            {
                TrainingId = training.Id,
                TrainingTitle = training.Title,
                TrainingType = training.Type,
                AssignedDate = DateTime.Today
            };

            await LoadAssignListsAsync(model);

            if (model.Years.Count == 0)
            {
                TempData["Error"] = "You don't have any active years available.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignTrainingViewModel model)
        {
            var training = await _context.Trainings.FindAsync(model.TrainingId);

            if (training == null)
                return NotFound();

            model.TrainingTitle = training.Title;
            model.TrainingType = training.Type;

            var allowed = await GetAllowedYearIdsAsync();

            if (!CanAccessYear(allowed, model.YearId))
                return Forbid();

            var group = await _context.Groups.FirstOrDefaultAsync(g =>
                g.Id == model.GroupId &&
                g.YearId == model.YearId &&
                g.IsActive);

            if (group == null)
                ModelState.AddModelError(nameof(model.GroupId),
                    "Selected group does not belong to the selected year.");

            if (training.Type == "Online")
            {
                if (model.StartDateTime == null || model.EndDateTime == null)
                {
                    ModelState.AddModelError("",
                        "Start and end date/time are required for online trainings.");
                }
                else if (model.EndDateTime <= model.StartDateTime)
                {
                    ModelState.AddModelError("",
                        "End date/time must be after the start date/time.");
                }
            }
            else
            {
                model.StartDateTime = null;
                model.EndDateTime = null;
            }

            var date = model.AssignedDate.Date;

            if (group != null && await _context.TrainingAssignments.AnyAsync(a =>
                    a.TrainingId == training.Id &&
                    a.GroupId == group.Id &&
                    a.AssignedDate == date))
            {
                ModelState.AddModelError("",
                    "This training is already assigned to this group on that date.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAssignListsAsync(model);
                return View(model);
            }

            _context.TrainingAssignments.Add(new TrainingAssignment
            {
                TrainingId = training.Id,
                GroupId = group!.Id,
                AssignedDate = date,
                StartDateTime = model.StartDateTime,
                EndDateTime = model.EndDateTime
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Training assigned.";
            return RedirectToAction(nameof(Assignments));
        }

        // Groups of a year (used by the Assign page)
        [HttpGet]
        public async Task<IActionResult> GetGroupsByYear(int yearId)
        {
            var allowed = await GetAllowedYearIdsAsync();

            if (!CanAccessYear(allowed, yearId))
                return Forbid();

            var groups = await _context.Groups
                .Where(g => g.YearId == yearId && g.IsActive)
                .OrderBy(g => g.Name)
                .Select(g => new { id = g.Id, name = g.Name })
                .ToListAsync();

            return Json(groups);
        }

        // =====================================================
        // Grades
        // =====================================================

        public async Task<IActionResult> Grade(int? id)
        {
            if (id == null)
                return NotFound();

            var assignment = await LoadAssignmentAsync(id.Value);

            if (assignment == null)
                return NotFound();

            if (!CanAccessYear(await GetAllowedYearIdsAsync(), assignment.Group.YearId))
                return Forbid();

            Dictionary<int, decimal?> existing;

            if (assignment.Training.Type == "InClass")
            {
                existing = await _context.TrainingRecords
                    .Where(r => r.TrainingAssignmentId == assignment.Id)
                    .ToDictionaryAsync(r => r.StudentId, r => (decimal?)r.Score);
            }
            else
            {
                // Online training: scores live on the student's attempt.
                var attempts = await _context.TrainingAttempts
                    .Where(a => a.TrainingAssignmentId == assignment.Id && a.SubmittedAt != null)
                    .ToListAsync();
                existing = attempts
                    .GroupBy(a => a.StudentId)
                    .ToDictionary(g => g.Key, g => (decimal?)g.OrderByDescending(a => a.StartedAt).First().Score);
            }

            return View(await BuildGradeModelAsync(assignment, existing));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(GradeTrainingViewModel model)
        {
            var assignment = await LoadAssignmentAsync(model.AssignmentId);

            if (assignment == null)
                return NotFound();

            if (!CanAccessYear(await GetAllowedYearIdsAsync(), assignment.Group.YearId))
                return Forbid();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var activeStudentIds = await _context.Enrollments
                .Where(e => e.GroupId == assignment.GroupId && e.Status == "Active")
                .Select(e => e.StudentId)
                .ToListAsync();

            var submitted = (model.Students ?? new List<GradeStudentItem>())
                .Where(s => activeStudentIds.Contains(s.StudentId))
                .GroupBy(s => s.StudentId)
                .Select(g => g.First())
                .ToList();

            var max = assignment.Training.MaximumGrade;

            foreach (var s in submitted)
            {
                if (s.Score != null && (s.Score < 0 || s.Score > max))
                {
                    ModelState.AddModelError("",
                        $"Grades must be between 0 and {max:0.##}.");
                    break;
                }
            }

            if (!ModelState.IsValid)
            {
                var posted = submitted.ToDictionary(s => s.StudentId, s => s.Score);
                return View(await BuildGradeModelAsync(assignment, posted));
            }

            if (assignment.Training.Type != "InClass")
            {
                await SaveOnlineGradesAsync(assignment, submitted, max);
                TempData["Success"] = "Grades saved.";
                return RedirectToAction(nameof(Assignments));
            }

            var records = await _context.TrainingRecords
                .Where(r => r.TrainingAssignmentId == assignment.Id)
                .ToDictionaryAsync(r => r.StudentId);

            foreach (var s in submitted)
            {
                // Empty field = leave as it is (no grade is added or removed)
                if (s.Score == null)
                    continue;

                if (records.TryGetValue(s.StudentId, out var record))
                {
                    record.Score = s.Score.Value;
                    record.MaxScore = max;
                    record.RecordedAt = DateTime.UtcNow;
                    record.RecordedByUserId = userId;
                }
                else
                {
                    _context.TrainingRecords.Add(new TrainingRecord
                    {
                        TrainingAssignmentId = assignment.Id,
                        StudentId = s.StudentId,
                        Score = s.Score.Value,
                        MaxScore = max,
                        RecordedAt = DateTime.UtcNow,
                        RecordedByUserId = userId
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Grades saved.";
            return RedirectToAction(nameof(Assignments));
        }

        // =====================================================
        // Helpers
        // =====================================================

        private static bool IsValidType(string? type) =>
            type == "InClass" || type == "Online";

        // null  = all years (Admin)
        // empty = no years (inactive or missing assistant)
        private async Task<List<int>?> GetAllowedYearIdsAsync()
        {
            if (User.IsInRole("Admin"))
                return null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return new List<int>();

            var assistant = await _context.Assistants
                .FirstOrDefaultAsync(a =>
                    a.ApplicationUserId == userId && a.Status == "Active");

            if (assistant == null)
                return new List<int>();

            return await _context.AssistantYears
                .Where(ay => ay.AssistantId == assistant.Id)
                .Select(ay => ay.YearId)
                .ToListAsync();
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

        private static bool CanAccessYear(List<int>? allowed, int yearId) =>
            allowed == null || allowed.Contains(yearId);

        private async Task LoadAssignListsAsync(AssignTrainingViewModel model)
        {
            var allowed = await GetAllowedYearIdsAsync();

            var years = _context.Years.Where(y => y.IsActive);

            if (allowed != null)
                years = years.Where(y => allowed.Contains(y.Id));

            model.Years = await years
                .OrderBy(y => y.Name)
                .Select(y => new SelectListItem
                {
                    Value = y.Id.ToString(),
                    Text = y.Name,
                    Selected = y.Id == model.YearId
                })
                .ToListAsync();

            model.Groups = new List<SelectListItem>();

            if (model.YearId > 0 && CanAccessYear(allowed, model.YearId))
            {
                model.Groups = await _context.Groups
                    .Where(g => g.YearId == model.YearId && g.IsActive)
                    .OrderBy(g => g.Name)
                    .Select(g => new SelectListItem
                    {
                        Value = g.Id.ToString(),
                        Text = g.Name,
                        Selected = g.Id == model.GroupId
                    })
                    .ToListAsync();
            }
        }

        private Task<TrainingAssignment?> LoadAssignmentAsync(int id) =>
            _context.TrainingAssignments
                .Include(a => a.Training)
                .Include(a => a.Group)
                    .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(a => a.Id == id);

        // Online trainings keep the grade on the student's TrainingAttempt.
        // A student with no attempt gets one created; a graded attempt counts as submitted.
        private async Task SaveOnlineGradesAsync(
            TrainingAssignment assignment,
            List<GradeStudentItem> submitted,
            decimal max)
        {
            var existing = await _context.TrainingAttempts
                .Where(a => a.TrainingAssignmentId == assignment.Id)
                .ToListAsync();
            var latest = existing
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartedAt).First());
            var now = DateTime.UtcNow;

            foreach (var s in submitted)
            {
                if (s.Score == null)
                    continue;

                if (latest.TryGetValue(s.StudentId, out var attempt))
                {
                    attempt.Score = s.Score.Value;
                    attempt.MaxScore = max;
                    attempt.SubmittedAt ??= now;
                }
                else
                {
                    _context.TrainingAttempts.Add(new TrainingAttempt
                    {
                        TrainingAssignmentId = assignment.Id,
                        StudentId = s.StudentId,
                        Score = s.Score.Value,
                        MaxScore = max,
                        StartedAt = now,
                        SubmittedAt = now
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<GradeTrainingViewModel> BuildGradeModelAsync(
            TrainingAssignment assignment,
            Dictionary<int, decimal?> scores)
        {
            var students = await _context.Enrollments
                .Where(e => e.GroupId == assignment.GroupId && e.Status == "Active")
                .Select(e => new { e.StudentId, e.Student.FullName })
                .OrderBy(s => s.FullName)
                .ToListAsync();

            return new GradeTrainingViewModel
            {
                AssignmentId = assignment.Id,
                TrainingTitle = assignment.Training.Title,
                YearName = assignment.Group.Year.Name,
                GroupName = assignment.Group.Name,
                AssignedDate = assignment.AssignedDate,
                MaxGrade = assignment.Training.MaximumGrade,
                Students = students.Select(s => new GradeStudentItem
                {
                    StudentId = s.StudentId,
                    FullName = s.FullName,
                    Score = scores.TryGetValue(s.StudentId, out var score) ? score : null
                }).ToList()
            };
        }
    }
}
