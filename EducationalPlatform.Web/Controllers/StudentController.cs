using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // Student Dashboard
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var student = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Group)
                    .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
                return NotFound("Student profile was not found.");

            ViewBag.StudentName = student.FullName;

            var vm = new StudentDashboardViewModel
            {
                StudentName = student.FullName
            };

            var enrollment = student.Enrollments
                .FirstOrDefault(e => e.Status == "Active");

            if (enrollment == null)
                return View(vm);

            vm.HasEnrollment = true;
            vm.YearName = enrollment.Group.Year.Name;
            vm.GroupName = enrollment.Group.Name;

            var groupId = enrollment.GroupId;
            var yearId = enrollment.Group.YearId;
            var now = DateTime.Now;
            var today = now.Date;

            // ---------- Attendance ----------
            var sessions = await _context.ClassSessions
                .Where(s => s.GroupId == groupId)
                .ToListAsync();

            var attendanceRecords = await _context.AttendanceRecords
                .Where(a => a.StudentId == student.Id)
                .ToDictionaryAsync(a => a.ClassSessionId, a => a.Status);

            foreach (var session in sessions)
            {
                if (session.Status == "Cancelled")
                {
                    vm.CancelledCount++;
                    continue;
                }

                if (attendanceRecords.TryGetValue(session.Id, out var status))
                {
                    switch (status)
                    {
                        case "Present": vm.PresentCount++; break;
                        case "Absent": vm.AbsentCount++; break;
                        case "Late": vm.LateCount++; break;
                        case "Excused": vm.ExcusedCount++; break;
                    }
                }
                else if (session.Status == "Completed")
                {
                    // Completed session without a record = Absent
                    vm.AbsentCount++;
                }
                else
                {
                    vm.NotRecordedCount++;
                }
            }

            var recorded = vm.PresentCount + vm.LateCount + vm.AbsentCount + vm.ExcusedCount;
            if (recorded > 0)
                vm.AttendanceRate = (int)Math.Round(
                    (vm.PresentCount + vm.LateCount) * 100.0 / recorded);

            // ---------- Next session ----------
            var next = sessions
                .Where(s => s.Status == "Scheduled" &&
                            (s.Date.Date > today ||
                             (s.Date.Date == today && s.EndTime > now.TimeOfDay)))
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .FirstOrDefault();

            if (next != null)
            {
                vm.NextSessionDate = next.Date;
                vm.NextSessionStart = next.StartTime;
                vm.NextSessionEnd = next.EndTime;
            }

            // ---------- Upcoming exams ----------
            var exams = await _context.ExamAssignments
                .Where(a => a.GroupId == groupId && a.ExamDate >= today)
                .OrderBy(a => a.ExamDate).ThenBy(a => a.StartTime)
                .Take(3)
                .Select(a => new { a.Exam.Title, a.ExamDate, a.StartTime, a.EndTime })
                .ToListAsync();

            vm.UpcomingExams = exams.Select(e => new StudentDashboardEvent
            {
                Title = e.Title,
                Start = e.ExamDate.Date.Add(e.StartTime),
                End = e.ExamDate.Date.Add(e.EndTime)
            }).ToList();

            // ---------- Upcoming / open quizzes ----------
            var quizzes = await _context.QuizAssignments
                .Where(a => a.GroupId == groupId && a.EndDateTime >= now)
                .OrderBy(a => a.StartDateTime)
                .Take(3)
                .Select(a => new
                {
                    a.Quiz.Title,
                    a.StartDateTime,
                    a.EndDateTime,
                    Submitted = a.QuizAttempts.Any(t =>
                        t.StudentId == student.Id && t.SubmittedAt != null)
                })
                .ToListAsync();

            vm.UpcomingQuizzes = quizzes.Select(q => new StudentDashboardEvent
            {
                Title = q.Title,
                Start = q.StartDateTime,
                End = q.EndDateTime,
                IsOpen = q.StartDateTime <= now,
                Submitted = q.Submitted
            }).ToList();

            // ---------- Latest grades ----------
            var examGrades = await _context.ExamResults
                .Where(r => r.StudentId == student.Id)
                .OrderByDescending(r => r.RecordedAt)
                .Take(5)
                .Select(r => new StudentDashboardGrade
                {
                    Kind = "Exam",
                    Title = r.ExamAssignment.Exam.Title,
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    Date = r.RecordedAt
                })
                .ToListAsync();

            var quizGrades = await _context.QuizAttempts
                .Where(a => a.StudentId == student.Id && a.SubmittedAt != null)
                .OrderByDescending(a => a.SubmittedAt)
                .Take(5)
                .Select(a => new StudentDashboardGrade
                {
                    Kind = "Quiz",
                    Title = a.QuizAssignment.Quiz.Title,
                    Score = a.Score,
                    MaxScore = a.MaxScore,
                    Date = a.SubmittedAt ?? a.StartedAt
                })
                .ToListAsync();

            var trainingRecordGrades = await _context.TrainingRecords
                .Where(r => r.StudentId == student.Id)
                .OrderByDescending(r => r.RecordedAt)
                .Take(5)
                .Select(r => new StudentDashboardGrade
                {
                    Kind = "Training",
                    Title = r.TrainingAssignment.Training.Title,
                    Score = r.Score,
                    MaxScore = r.MaxScore,
                    Date = r.RecordedAt
                })
                .ToListAsync();

            var trainingAttemptGrades = await _context.TrainingAttempts
                .Where(a => a.StudentId == student.Id && a.SubmittedAt != null)
                .OrderByDescending(a => a.SubmittedAt)
                .Take(5)
                .Select(a => new StudentDashboardGrade
                {
                    Kind = "Training",
                    Title = a.TrainingAssignment.Training.Title,
                    Score = a.Score,
                    MaxScore = a.MaxScore,
                    Date = a.SubmittedAt ?? a.StartedAt
                })
                .ToListAsync();

            vm.LatestGrades = examGrades
                .Concat(quizGrades)
                .Concat(trainingRecordGrades)
                .Concat(trainingAttemptGrades)
                .OrderByDescending(g => g.Date)
                .Take(5)
                .ToList();

            // ---------- Latest posts for the student's year ----------
            vm.LatestPosts = await _context.Posts
                .Where(p => p.YearId == yearId && p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .Select(p => new StudentDashboardPost
                {
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return View(vm);
        }

        // =====================================================
        // Student Attendance History
        // =====================================================
        public async Task<IActionResult> Attendance()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var student = await _context.Students
                .Include(s => s.Enrollments)
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
                return NotFound("Student profile was not found.");

            ViewBag.StudentName = student.FullName;

            // Get the student's current enrollment
            var enrollment = student.Enrollments
                .FirstOrDefault(e => e.Status == "Active");

            if (enrollment == null)
            {
                return View(new List<StudentAttendanceViewModel>());
            }

            var sessions = await _context.ClassSessions
                .Include(s => s.Group)
                    .ThenInclude(g => g.Year)
                .Where(s => s.GroupId == enrollment.GroupId)
                .OrderByDescending(s => s.Date)
                .ThenByDescending(s => s.StartTime)
                .ToListAsync();

            var attendanceRecords = await _context.AttendanceRecords
                .Where(a => a.StudentId == student.Id)
                .ToDictionaryAsync(a => a.ClassSessionId, a => a.Status);

            var model = sessions.Select(session =>
            {
                string attendanceStatus;

                if (session.Status == "Cancelled")
                {
                    attendanceStatus = "Cancelled";
                }
                else if (attendanceRecords.TryGetValue(session.Id, out var recordedStatus))
                {
                    attendanceStatus = recordedStatus;
                }
                else if (session.Status == "Completed")
                {
                    attendanceStatus = "Absent";
                }
                else
                {
                    attendanceStatus = "Not Recorded";
                }

                return new StudentAttendanceViewModel
                {
                    SessionId = session.Id,
                    YearName = session.Group.Year.Name,
                    GroupName = session.Group.Name,
                    Date = session.Date,
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    SessionStatus = session.Status,
                    AttendanceStatus = attendanceStatus
                };
            }).ToList();

            return View(model);
        }

        // =====================================================
        // My Exams (with scores)
        // =====================================================
        public async Task<IActionResult> Exams()
        {
            var (student, enrollment) = await LoadStudentAsync();
            if (student == null)
                return NotFound("Student profile was not found.");

            ViewBag.StudentName = student.FullName;
            int? groupId = enrollment?.GroupId;

            // Latest result per exam assignment
            var results = await _context.ExamResults
                .Where(r => r.StudentId == student.Id)
                .ToListAsync();

            var latest = results
                .GroupBy(r => r.ExamAssignmentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.RecordedAt).First());

            var gradedIds = latest.Keys.ToList();

            // Exams of the current group + any exam the student already has a result for
            var assignments = await _context.ExamAssignments
                .Include(a => a.Exam)
                .Where(a => a.GroupId == groupId || gradedIds.Contains(a.Id))
                .OrderBy(a => a.ExamDate)
                .ThenBy(a => a.StartTime)
                .ToListAsync();

            var now = DateTime.Now;

            var rows = assignments.Select(a =>
            {
                latest.TryGetValue(a.Id, out var result);

                string status;
                if (result != null)
                {
                    var max = result.MaxScore > 0 ? result.MaxScore : a.Exam.MaximumGrade;
                    var percent = max > 0 ? result.Score * 100 / max : 0;
                    status = percent >= a.Exam.PassingPercentage ? "Passed" : "Failed";
                }
                else if (a.ExamDate.Date > now.Date ||
                        (a.ExamDate.Date == now.Date && a.EndTime > now.TimeOfDay))
                {
                    status = "Upcoming";
                }
                else
                {
                    status = "Not graded yet";
                }

                return new StudentExamRow
                {
                    Title = a.Exam.Title,
                    Description = a.Exam.Description,
                    Date = a.ExamDate,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    MaximumGrade = a.Exam.MaximumGrade,
                    PassingPercentage = a.Exam.PassingPercentage,
                    Score = result?.Score,
                    MaxScore = result != null && result.MaxScore > 0
                        ? result.MaxScore
                        : a.Exam.MaximumGrade,
                    Status = status
                };
            }).ToList();

            return View(rows);
        }

        // =====================================================
        // My Quizzes (with scores)
        // =====================================================
        public async Task<IActionResult> Quizzes()
        {
            var (student, enrollment) = await LoadStudentAsync();
            if (student == null)
                return NotFound("Student profile was not found.");

            ViewBag.StudentName = student.FullName;
            int? groupId = enrollment?.GroupId;

            var attempts = await _context.QuizAttempts
                .Where(a => a.StudentId == student.Id)
                .ToListAsync();

            var latest = attempts
                .GroupBy(a => a.QuizAssignmentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartedAt).First());

            var attemptedIds = latest.Keys.ToList();

            var assignments = await _context.QuizAssignments
                .Include(a => a.Quiz)
                .Where(a => a.GroupId == groupId || attemptedIds.Contains(a.Id))
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();

            var now = DateTime.Now;

            var rows = assignments.Select(a =>
            {
                latest.TryGetValue(a.Id, out var attempt);

                string status;
                if (attempt != null && attempt.SubmittedAt != null)
                    status = attempt.MaxScore > 0 ? "Graded" : "Awaiting grading";
                else if (attempt != null)
                    status = "In progress";
                else if (now < a.StartDateTime)
                    status = "Upcoming";
                else if (now <= a.EndDateTime)
                    status = "Open";
                else
                    status = "Missed";

                var graded = attempt != null && attempt.SubmittedAt != null && attempt.MaxScore > 0;

                return new StudentQuizRow
                {
                    Title = a.Quiz.Title,
                    Description = a.Quiz.Description,
                    Start = a.StartDateTime,
                    End = a.EndDateTime,
                    DurationMinutes = a.Quiz.DurationMinutes,
                    MaximumGrade = a.Quiz.MaximumGrade,
                    Score = graded ? attempt!.Score : null,
                    MaxScore = graded ? attempt!.MaxScore : a.Quiz.MaximumGrade,
                    Status = status
                };
            }).ToList();

            return View(rows);
        }

        // =====================================================
        // My Trainings (with scores)
        // =====================================================
        public async Task<IActionResult> Trainings()
        {
            var (student, enrollment) = await LoadStudentAsync();
            if (student == null)
                return NotFound("Student profile was not found.");

            ViewBag.StudentName = student.FullName;
            int? groupId = enrollment?.GroupId;

            // In-class trainings are graded by the assistant (TrainingRecord)
            var records = (await _context.TrainingRecords
                    .Where(r => r.StudentId == student.Id)
                    .ToListAsync())
                .GroupBy(r => r.TrainingAssignmentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.RecordedAt).First());

            // Online trainings are attempted by the student (TrainingAttempt)
            var attempts = (await _context.TrainingAttempts
                    .Where(a => a.StudentId == student.Id)
                    .ToListAsync())
                .GroupBy(a => a.TrainingAssignmentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartedAt).First());

            var knownIds = records.Keys.Union(attempts.Keys).ToList();

            var assignments = await _context.TrainingAssignments
                .Include(a => a.Training)
                .Where(a => a.GroupId == groupId || knownIds.Contains(a.Id))
                .OrderBy(a => a.AssignedDate)
                .ThenBy(a => a.Id)
                .ToListAsync();

            var rows = assignments.Select(a =>
            {
                records.TryGetValue(a.Id, out var record);
                attempts.TryGetValue(a.Id, out var attempt);

                decimal? score = null;
                decimal maxScore = a.Training.MaximumGrade;
                string status;

                if (record != null)
                {
                    score = record.Score;
                    maxScore = record.MaxScore > 0 ? record.MaxScore : a.Training.MaximumGrade;
                    status = "Graded";
                }
                else if (attempt != null && attempt.SubmittedAt != null)
                {
                    if (attempt.MaxScore > 0)
                    {
                        score = attempt.Score;
                        maxScore = attempt.MaxScore;
                        status = "Graded";
                    }
                    else
                    {
                        status = "Awaiting grading";
                    }
                }
                else if (attempt != null)
                {
                    status = "In progress";
                }
                else
                {
                    status = "Not graded yet";
                }

                return new StudentTrainingRow
                {
                    Title = a.Training.Title,
                    Description = a.Training.Description,
                    Type = a.Training.Type,
                    AssignedDate = a.AssignedDate,
                    MaximumGrade = a.Training.MaximumGrade,
                    Score = score,
                    MaxScore = maxScore,
                    Status = status
                };
            }).ToList();

            return View(rows);
        }

        // =====================================================
        // Posts for the student's year
        // =====================================================
        public async Task<IActionResult> Posts()
        {
            var (student, enrollment) = await LoadStudentAsync();
            if (student == null)
                return NotFound("Student profile was not found.");

            ViewBag.StudentName = student.FullName;

            var posts = new List<Post>();

            if (enrollment != null)
            {
                var yearId = enrollment.Group.YearId;

                posts = await _context.Posts
                    .Include(p => p.Year)
                    .Include(p => p.Attachments)
                    .Where(p => p.YearId == yearId && p.IsPublished)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();
            }

            return View(posts);
        }

        // =====================================================
        // Helper: the logged-in student + the active enrollment
        // =====================================================
        private async Task<(Student? Student, Enrollment? Enrollment)> LoadStudentAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var student = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Group)
                    .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            var enrollment = student?.Enrollments
                .FirstOrDefault(e => e.Status == "Active");

            return (student, enrollment);
        }
    }
}
