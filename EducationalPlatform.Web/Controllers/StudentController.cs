
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

        // Student Dashboard
        //public async Task<IActionResult> Index()
        //{
        //    var userId = User.FindFirstValue(
        //        ClaimTypes.NameIdentifier);

        //    var student = await _context.Students
        //        .Include(s => s.Enrollments)
        //            .ThenInclude(e => e.Group)
        //            .ThenInclude(g => g.Year)
        //        .FirstOrDefaultAsync(s =>
        //            s.ApplicationUserId == userId);

        //    if (student == null)
        //        return NotFound("Student profile was not found.");

        //    var attendance = await _context.AttendanceRecords
        //        .Where(a => a.StudentId == student.Id)
        //        .ToListAsync();

        //    ViewBag.StudentName = student.FullName;

        //    ViewBag.CurrentEnrollment = student.Enrollments
        //        .FirstOrDefault(e => e.Status == "Active");

        //    ViewBag.TotalSessions = attendance.Count;

        //    ViewBag.PresentCount = attendance.Count(a =>
        //        a.Status == "Present");

        //    ViewBag.AbsentCount = attendance.Count(a =>
        //        a.Status == "Absent");

        //    ViewBag.LateCount = attendance.Count(a =>
        //        a.Status == "Late");

        //    ViewBag.ExcusedCount = attendance.Count(a =>
        //        a.Status == "Excused");

        //    return View();
        //}




        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var student = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Group)
                    .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(s =>
                    s.ApplicationUserId == userId);

            if (student == null)
                return NotFound("Student profile was not found.");

            ViewBag.StudentName = student.FullName;

            var enrollment = student.Enrollments
                .FirstOrDefault(e => e.Status == "Active");

            ViewBag.CurrentEnrollment = enrollment;

            // Get all sessions for the student's current group
            var sessions = new List<ClassSession>();

            if (enrollment != null)
            {
                sessions = await _context.ClassSessions
                    .Where(s => s.GroupId == enrollment.GroupId)
                    .ToListAsync();
            }

            // Get the student's recorded attendance
            var attendanceRecords = await _context.AttendanceRecords
                .Where(a => a.StudentId == student.Id)
                .ToDictionaryAsync(a => a.ClassSessionId, a => a.Status);

            int presentCount = 0;
            int absentCount = 0;
            int lateCount = 0;
            int excusedCount = 0;
            int notRecordedCount = 0;
            int cancelledCount = 0;

            foreach (var session in sessions)
            {
                if (session.Status == "Cancelled")
                {
                    cancelledCount++;
                    continue;
                }

                if (attendanceRecords.TryGetValue(
                    session.Id, out var status))
                {
                    switch (status)
                    {
                        case "Present":
                            presentCount++;
                            break;

                        case "Absent":
                            absentCount++;
                            break;

                        case "Late":
                            lateCount++;
                            break;

                        case "Excused":
                            excusedCount++;
                            break;
                    }
                }
                else if (session.Status == "Completed")
                {
                    // Completed session without a record = Absent
                    absentCount++;
                }
                else
                {
                    // Scheduled session without a record
                    notRecordedCount++;
                }
            }

            ViewBag.TotalSessions = sessions.Count;
            ViewBag.PresentCount = presentCount;
            ViewBag.AbsentCount = absentCount;
            ViewBag.LateCount = lateCount;
            ViewBag.ExcusedCount = excusedCount;
            ViewBag.NotRecordedCount = notRecordedCount;
            ViewBag.CancelledCount = cancelledCount;

            return View();
        }





        // Student Attendance History
        //public async Task<IActionResult> Attendance()
        //{
        //    var userId = User.FindFirstValue(
        //        ClaimTypes.NameIdentifier);

        //    var student = await _context.Students
        //        .FirstOrDefaultAsync(s =>
        //            s.ApplicationUserId == userId);

        //    if (student == null)
        //        return NotFound("Student profile was not found.");

        //    var records = await _context.AttendanceRecords
        //        .Where(a => a.StudentId == student.Id)
        //        .Include(a => a.ClassSession)
        //            .ThenInclude(s => s.Group)
        //            .ThenInclude(g => g.Year)
        //        .OrderByDescending(a => a.ClassSession.Date)
        //        .ThenByDescending(a => a.ClassSession.StartTime)
        //        .ToListAsync();

        //    return View(records);
        //}



        public async Task<IActionResult> Attendance()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var student = await _context.Students
                .Include(s => s.Enrollments)
                .FirstOrDefaultAsync(s =>
                    s.ApplicationUserId == userId);

            if (student == null)
                return NotFound("Student profile was not found.");

            // Get the student's current enrollment
            var enrollment = student.Enrollments
                .FirstOrDefault(e => e.Status == "Active");

            if (enrollment == null)
            {
                return View(
                    new List<StudentAttendanceViewModel>());
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
                else if (attendanceRecords.TryGetValue(
                    session.Id, out var recordedStatus))
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
    }
}