using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Assistant")]
    public class AssistantDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AssistantDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
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
    }
}
