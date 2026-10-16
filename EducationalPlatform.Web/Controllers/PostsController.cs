using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.Services;
using EducationalPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Controllers
{
    [Authorize(Roles = "Admin,Assistant")]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private const long MaxFileSize = 100 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp",
                ".mp4", ".webm", ".mov",
                ".pdf", ".doc", ".docx",
                ".xls", ".xlsx", ".ppt", ".pptx",
                ".txt", ".zip"
            };

        private readonly INotificationService _notifications;

        public PostsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            INotificationService notifications)
        {
            _context = context;
            _environment = environment;
            _notifications = notifications;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        // Returns all school years available to the current user.
        private async Task<List<Year>> GetAllowedYearsAsync()
        {
            if (User.IsInRole("Admin"))
            {
                return await _context.Years
                    .Where(y => y.IsActive)
                    .OrderBy(y => y.Name)
                    .ToListAsync();
            }

            var userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return new List<Year>();

            return await _context.Assistants
                .Where(a =>
                    a.ApplicationUserId == userId &&
                    a.Status == "Active")
                .SelectMany(a => a.AssistantYears)
                .Where(ay => ay.Year.IsActive)
                .Select(ay => ay.Year)
                .Distinct()
                .OrderBy(y => y.Name)
                .ToListAsync();
        }

        // Checks whether the current user can access a specific year.
        private async Task<bool> CanAccessYearAsync(int yearId)
        {
            var years = await GetAllowedYearsAsync();

            return years.Any(y => y.Id == yearId);
        }

        // Loads the school years into a dropdown list.
        private async Task LoadYearsAsync(int? selectedYearId = null)
        {
            var years = await GetAllowedYearsAsync();

            ViewBag.Years = new SelectList(
                years,
                "Id",
                "Name",
                selectedYearId);
        }

        // Validates file extensions and sizes, adding errors to ModelState.
        private void ValidateFiles(List<IFormFile> files)
        {
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName);

                if (!AllowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "Files",
                        $"The file type of {file.FileName} is not allowed.");
                }

                if (file.Length > MaxFileSize)
                {
                    ModelState.AddModelError(
                        "Files",
                        $"The file {file.FileName} exceeds the 100 MB limit.");
                }
            }
        }

        private string GetUploadFolder()
        {
            return Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "posts");
        }

        // Maps an attachment's stored web path to its physical path.
        // Uses only the file name, so a tampered path can't escape the folder.
        private string GetPhysicalPath(PostAttachment attachment)
        {
            return Path.Combine(
                GetUploadFolder(),
                Path.GetFileName(attachment.FilePath));
        }

        private static void DeleteFilesQuietly(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                try
                {
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }
                catch
                {
                    // A leftover file must not break the request.
                }
            }
        }

        // ---------------------------------------------------------------
        // Index
        // ---------------------------------------------------------------

        // Displays posts, optionally filtered by school year.
        public async Task<IActionResult> Index(int? yearId)
        {
            var years = await GetAllowedYearsAsync();

            if (yearId.HasValue &&
                !years.Any(y => y.Id == yearId.Value))
            {
                return Forbid();
            }

            await LoadYearsAsync(yearId);

            var posts = _context.Posts
                .Include(p => p.Year)
                .Include(p => p.Attachments)
                .Include(p => p.CreatedByUser)
                .AsQueryable();

            if (yearId.HasValue)
            {
                posts = posts.Where(p => p.YearId == yearId.Value);
            }
            else
            {
                var allowedIds = years.Select(y => y.Id).ToList();

                posts = posts.Where(
                    p => allowedIds.Contains(p.YearId));
            }

            ViewBag.SelectedYearId = yearId;

            return View(await posts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync());
        }

        // ---------------------------------------------------------------
        // Create
        // ---------------------------------------------------------------

        // Opens the Create Post page.
        [HttpGet]
        public async Task<IActionResult> Create(int? yearId)
        {
            var years = await GetAllowedYearsAsync();

            if (years.Count == 0)
            {
                TempData["Error"] =
                    "No active school years are available for your account.";

                return RedirectToAction(nameof(Index));
            }

            if (yearId.HasValue &&
                !years.Any(y => y.Id == yearId.Value))
            {
                return Forbid();
            }

            var model = new PostCreateViewModel
            {
                YearId = yearId ?? years[0].Id
            };

            await LoadYearsAsync(model.YearId);

            return View(model);
        }

        // Creates a post and saves its attachments.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PostCreateViewModel model)
        {
            if (!await CanAccessYearAsync(model.YearId))
                return Forbid();

            var files = model.Files
                .Where(f => f != null && f.Length > 0)
                .ToList();

            ValidateFiles(files);

            if (!ModelState.IsValid)
            {
                await LoadYearsAsync(model.YearId);

                return View(model);
            }

            var userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var post = new Post
            {
                YearId = model.YearId,
                Title = model.Title.Trim(),
                Content = model.Content.Trim(),
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsPublished = true
            };

            var uploadFolder = GetUploadFolder();

            Directory.CreateDirectory(uploadFolder);

            var savedFiles = new List<string>();

            try
            {
                foreach (var file in files)
                {
                    var extension = Path.GetExtension(file.FileName);

                    var storedFileName =
                        $"{Guid.NewGuid():N}{extension}";

                    var fullPath = Path.Combine(
                        uploadFolder,
                        storedFileName);

                    await using (var stream = new FileStream(
                        fullPath,
                        FileMode.CreateNew))
                    {
                        await file.CopyToAsync(stream);
                    }

                    savedFiles.Add(fullPath);

                    post.Attachments.Add(new PostAttachment
                    {
                        FileName = Path.GetFileName(file.FileName),
                        FilePath = $"/uploads/posts/{storedFileName}",
                        ContentType = file.ContentType,
                        FileSize = file.Length
                    });
                }

                _context.Posts.Add(post);

                await _context.SaveChangesAsync();

                // Never throws: a notification problem must not undo the saved post.
                await _notifications.NotifyYearAsync(post.YearId, NotificationTypes.PostCreated,
                    "New post",
                    post.Title);

                TempData["Success"] =
                    "The post was created successfully.";

                return RedirectToAction(
                    nameof(Index),
                    new { yearId = model.YearId });
            }
            catch
            {
                DeleteFilesQuietly(savedFiles);

                ModelState.AddModelError(
                    string.Empty,
                    "An error occurred while saving the post or its attachments.");

                await LoadYearsAsync(model.YearId);

                return View(model);
            }
        }

        // ---------------------------------------------------------------
        // Edit
        // ---------------------------------------------------------------

        // Opens the Edit Post page.
        [HttpGet]
        public async Task<IActionResult> Edit(int id, int? returnYearId)
        {
            var post = await _context.Posts
                .Include(p => p.Attachments)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
                return NotFound();

            if (!await CanAccessYearAsync(post.YearId))
                return Forbid();

            var model = new PostEditViewModel
            {
                Id = post.Id,
                YearId = post.YearId,
                Title = post.Title,
                Content = post.Content,
                ExistingAttachments = post.Attachments.ToList()
            };

            ViewBag.ReturnYearId = returnYearId;

            await LoadYearsAsync(model.YearId);

            return View(model);
        }

        // Updates a post, removes selected attachments and adds new ones.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            PostEditViewModel model,
            int? returnYearId)
        {
            if (id != model.Id)
                return BadRequest();

            var post = await _context.Posts
                .Include(p => p.Attachments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
                return NotFound();

            // The user must have access to the current year AND the target year.
            if (!await CanAccessYearAsync(post.YearId) ||
                !await CanAccessYearAsync(model.YearId))
            {
                return Forbid();
            }

            var newFiles = (model.Files ?? new List<IFormFile>())
                .Where(f => f != null && f.Length > 0)
                .ToList();

            ValidateFiles(newFiles);

            if (!ModelState.IsValid)
            {
                model.ExistingAttachments = post.Attachments.ToList();
                ViewBag.ReturnYearId = returnYearId;

                await LoadYearsAsync(model.YearId);

                return View(model);
            }

            // Only attachments that really belong to this post can be removed.
            var removeIds = model.RemoveAttachmentIds ?? new List<int>();

            var attachmentsToRemove = post.Attachments
                .Where(a => removeIds.Contains(a.Id))
                .ToList();

            var pathsToDelete = attachmentsToRemove
                .Select(GetPhysicalPath)
                .ToList();

            var uploadFolder = GetUploadFolder();

            Directory.CreateDirectory(uploadFolder);

            var savedFiles = new List<string>();

            try
            {
                post.Title = model.Title.Trim();
                post.Content = model.Content.Trim();
                post.YearId = model.YearId;

                _context.RemoveRange(attachmentsToRemove);

                foreach (var file in newFiles)
                {
                    var extension = Path.GetExtension(file.FileName);

                    var storedFileName =
                        $"{Guid.NewGuid():N}{extension}";

                    var fullPath = Path.Combine(
                        uploadFolder,
                        storedFileName);

                    await using (var stream = new FileStream(
                        fullPath,
                        FileMode.CreateNew))
                    {
                        await file.CopyToAsync(stream);
                    }

                    savedFiles.Add(fullPath);

                    post.Attachments.Add(new PostAttachment
                    {
                        FileName = Path.GetFileName(file.FileName),
                        FilePath = $"/uploads/posts/{storedFileName}",
                        ContentType = file.ContentType,
                        FileSize = file.Length
                    });
                }

                await _context.SaveChangesAsync();

                // Delete the old files from disk only after the DB save succeeded.
                DeleteFilesQuietly(pathsToDelete);

                TempData["Success"] =
                    "The post was updated successfully.";

                return RedirectToAction(
                    nameof(Index),
                    new { yearId = returnYearId });
            }
            catch
            {
                DeleteFilesQuietly(savedFiles);

                // Discard the half-applied changes before re-reading from the DB.
                _context.ChangeTracker.Clear();

                model.ExistingAttachments = await _context.Posts
                    .Where(p => p.Id == id)
                    .SelectMany(p => p.Attachments)
                    .AsNoTracking()
                    .ToListAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "An error occurred while updating the post or its attachments.");

                ViewBag.ReturnYearId = returnYearId;

                await LoadYearsAsync(model.YearId);

                return View(model);
            }
        }

        // ---------------------------------------------------------------
        // Delete
        // ---------------------------------------------------------------

        // Deletes a post together with its attachments (DB rows and files).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int? returnYearId)
        {
            var post = await _context.Posts
                .Include(p => p.Attachments)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                TempData["Error"] = "The post was not found.";

                return RedirectToAction(
                    nameof(Index),
                    new { yearId = returnYearId });
            }

            if (!await CanAccessYearAsync(post.YearId))
                return Forbid();

            var pathsToDelete = post.Attachments
                .Select(GetPhysicalPath)
                .ToList();

            try
            {
                _context.RemoveRange(post.Attachments);
                _context.Posts.Remove(post);

                await _context.SaveChangesAsync();

                DeleteFilesQuietly(pathsToDelete);

                TempData["Success"] =
                    "The post was deleted successfully.";
            }
            catch
            {
                TempData["Error"] =
                    "An error occurred while deleting the post.";
            }

            return RedirectToAction(
                nameof(Index),
                new { yearId = returnYearId });
        }
    }
}