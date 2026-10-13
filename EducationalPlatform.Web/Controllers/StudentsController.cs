using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Students?yearId=1&groupId=2
        public async Task<IActionResult> Index(int? yearId, int? groupId)
        {
            // Ignore a group that does not belong to the selected year
            if (yearId.HasValue && groupId.HasValue)
            {
                var groupInYear = await _context.Groups
                    .AnyAsync(g => g.Id == groupId.Value && g.YearId == yearId.Value);

                if (!groupInYear)
                    groupId = null;
            }

            IQueryable<Student> query = _context.Students
                .Include(s => s.ApplicationUser)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Group)
                        .ThenInclude(g => g.Year);

            // Filter by the student's ACTIVE enrollment
            if (groupId.HasValue)
            {
                query = query.Where(s => s.Enrollments.Any(e =>
                    e.Status == "Active" && e.GroupId == groupId.Value));
            }
            else if (yearId.HasValue)
            {
                query = query.Where(s => s.Enrollments.Any(e =>
                    e.Status == "Active" && e.Group.YearId == yearId.Value));
            }

            var students = await query
                .OrderBy(s => s.FullName)
                .ToListAsync();

            // Filter dropdowns
            var years = await _context.Years
                .OrderBy(y => y.Name)
                .ToListAsync();

            ViewBag.Years = new SelectList(years, "Id", "Name", yearId);

            if (yearId.HasValue)
            {
                var groups = await _context.Groups
                    .Where(g => g.YearId == yearId.Value &&
                                (g.IsActive || g.Id == groupId))
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

            return View(students);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var student = await _context.Students
                .Include(s => s.ApplicationUser)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Group)
                        .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        public async Task<IActionResult> Create()
        {
            await LoadYearsAsync();

            return View(new StudentViewModel
            {
                JoinedAt = DateTime.Today,
                Status = "Active"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadYearsAsync(model.YearId);
                return View(model);
            }

            var group = await _context.Groups
                .Include(g => g.Year)
                .FirstOrDefaultAsync(g =>
                    g.Id == model.GroupId &&
                    g.YearId == model.YearId &&
                    g.IsActive &&
                    g.Year.IsActive);

            if (group == null)
            {
                ModelState.AddModelError(
                    "GroupId",
                    "Selected group does not belong to the selected year.");

                await LoadYearsAsync(model.YearId);
                return View(model);
            }

            var existingUser = await _userManager
                .FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                await LoadYearsAsync(model.YearId);
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Phone,
                EmailConfirmed = true
            };

            var userResult = await _userManager.CreateAsync(
                user,
                model.Password);

            if (!userResult.Succeeded)
            {
                foreach (var error in userResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                await LoadYearsAsync(model.YearId);
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Student");

            var student = new Student
            {
                ApplicationUserId = user.Id,
                FullName = model.FullName,
                DateOfBirth = model.DateOfBirth,
                Status = model.Status
            };

            _context.Students.Add(student);

            await _context.SaveChangesAsync();

            var enrollment = new Enrollment
            {
                StudentId = student.Id,
                GroupId = model.GroupId,
                JoinedAt = model.JoinedAt,
                Status = "Active"
            };

            _context.Enrollments.Add(enrollment);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Transfer(int? id)
        {
            if (id == null)
                return NotFound();

            var student = await _context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.Group)
                        .ThenInclude(g => g.Year)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            var currentEnrollment = student.Enrollments
                .FirstOrDefault(e => e.Status == "Active");

            if (currentEnrollment == null)
            {
                TempData["Error"] =
                    "This student does not have an active enrollment.";

                return RedirectToAction(nameof(Details), new { id });
            }

            await LoadYearsAsync();

            var model = new TransferStudentViewModel
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                CurrentGroupId = currentEnrollment.GroupId,
                CurrentYearName = currentEnrollment.Group.Year.Name,
                CurrentGroupName = currentEnrollment.Group.Name,
                TransferDate = DateTime.Today
            };

            return View(model);
        }
        //    [HttpPost]
        //    [ValidateAntiForgeryToken]
        //    public async Task<IActionResult> Transfer(
        //TransferStudentViewModel model)
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            await LoadYearsAsync(model.NewYearId);

        //            return View(model);
        //        }

        //        var student = await _context.Students
        //            .Include(s => s.Enrollments)
        //            .FirstOrDefaultAsync(s => s.Id == model.StudentId);

        //        if (student == null)
        //            return NotFound();

        //        var currentEnrollment = student.Enrollments
        //            .FirstOrDefault(e => e.Status == "Active");

        //        if (currentEnrollment == null)
        //        {
        //            ModelState.AddModelError(
        //                "",
        //                "Student does not have an active enrollment.");

        //            await LoadYearsAsync(model.NewYearId);

        //            return View(model);
        //        }

        //        var newGroup = await _context.Groups
        //            .Include(g => g.Year)
        //            .FirstOrDefaultAsync(g =>
        //                g.Id == model.NewGroupId &&
        //                g.YearId == model.NewYearId &&
        //                g.IsActive &&
        //                g.Year.IsActive);

        //        if (newGroup == null)
        //        {
        //            ModelState.AddModelError(
        //                "NewGroupId",
        //                "Selected group does not belong to the selected year.");

        //            await LoadYearsAsync(model.NewYearId);

        //            return View(model);
        //        }

        //        if (currentEnrollment.GroupId == model.NewGroupId)
        //        {
        //            ModelState.AddModelError(
        //                "NewGroupId",
        //                "Student is already enrolled in this group.");

        //            await LoadYearsAsync(model.NewYearId);

        //            return View(model);
        //        }

        //        if (model.TransferDate < currentEnrollment.JoinedAt)
        //        {
        //            ModelState.AddModelError(
        //                "TransferDate",
        //                "Transfer date cannot be before the current enrollment date.");

        //            await LoadYearsAsync(model.NewYearId);

        //            return View(model);
        //        }

        //        // Close old enrollment
        //        currentEnrollment.LeftAt = model.TransferDate;
        //        currentEnrollment.Status = "Transferred";

        //        // Create new enrollment
        //        var newEnrollment = new Enrollment
        //        {
        //            StudentId = student.Id,
        //            GroupId = model.NewGroupId,
        //            JoinedAt = model.TransferDate,
        //            Status = "Active"
        //        };

        //        _context.Enrollments.Add(newEnrollment);

        //        await _context.SaveChangesAsync();

        //        return RedirectToAction(
        //            nameof(Details),
        //            new { id = student.Id });
        //    }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(
            TransferStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadYearsAsync(model.NewYearId);

                return View(model);
            }

            var student = await _context.Students
                .Include(s => s.Enrollments)
                .FirstOrDefaultAsync(s => s.Id == model.StudentId);

            if (student == null)
                return NotFound();

            var currentEnrollment = student.Enrollments
                .FirstOrDefault(e => e.Status == "Active");

            if (currentEnrollment == null)
            {
                ModelState.AddModelError(
                    "",
                    "Student does not have an active enrollment.");

                await LoadYearsAsync(model.NewYearId);

                return View(model);
            }

            var newGroup = await _context.Groups
                .Include(g => g.Year)
                .FirstOrDefaultAsync(g =>
                    g.Id == model.NewGroupId &&
                    g.YearId == model.NewYearId &&
                    g.IsActive &&
                    g.Year.IsActive);

            if (newGroup == null)
            {
                ModelState.AddModelError(
                    "NewGroupId",
                    "Selected group is invalid.");

                await LoadYearsAsync(model.NewYearId);

                return View(model);
            }

            if (currentEnrollment.GroupId == model.NewGroupId)
            {
                ModelState.AddModelError(
                    "NewGroupId",
                    "Student is already enrolled in this group.");

                await LoadYearsAsync(model.NewYearId);

                return View(model);
            }

            if (model.TransferDate < currentEnrollment.JoinedAt)
            {
                ModelState.AddModelError(
                    "TransferDate",
                    "Transfer date cannot be before the current enrollment date.");

                await LoadYearsAsync(model.NewYearId);

                return View(model);
            }

            // Close current enrollment
            currentEnrollment.LeftAt = model.TransferDate;
            currentEnrollment.Status = "Transferred";

            // Create new enrollment
            var newEnrollment = new Enrollment
            {
                StudentId = student.Id,
                GroupId = model.NewGroupId,
                JoinedAt = model.TransferDate,
                Status = "Active"
            };

            _context.Enrollments.Add(newEnrollment);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id = student.Id });
        }

        public async Task<IActionResult> GetGroupsByYear(int yearId)
        {
            var groups = await _context.Groups
                .Where(g =>
                    g.YearId == yearId &&
                    g.IsActive)
                .OrderBy(g => g.Name)
                .Select(g => new
                {
                    id = g.Id,
                    name = g.Name
                })
                .ToListAsync();

            return Json(groups);
        }

        private async Task LoadYearsAsync(int? selectedYearId = null)
        {
            var years = await _context.Years
                .Where(y => y.IsActive)
                .OrderBy(y => y.Name)
                .ToListAsync();

            ViewBag.Years = new SelectList(
                years,
                "Id",
                "Name",
                selectedYearId);

            if (selectedYearId.HasValue)
            {
                var groups = await _context.Groups
                    .Where(g =>
                        g.YearId == selectedYearId.Value &&
                        g.IsActive)
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                ViewBag.Groups = new SelectList(
                    groups,
                    "Id",
                    "Name");
            }
            else
            {
                ViewBag.Groups = new SelectList(
                    Enumerable.Empty<SelectListItem>());
            }
        }
    }
}