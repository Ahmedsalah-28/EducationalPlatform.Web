using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Services
{
    // One notification for one student (used for per-student messages such as grades).
    public sealed record StudentNotification(int StudentId, string Type, string Title, string Message);

    // Notification types stored in Notification.Type, and the student page each one opens.
    public static class NotificationTypes
    {
        public const string ExamAssigned = "ExamAssigned";
        public const string QuizAssigned = "QuizAssigned";
        public const string TrainingAssigned = "TrainingAssigned";
        public const string PostCreated = "PostCreated";
        public const string ExamGrade = "ExamGrade";
        public const string QuizGrade = "QuizGrade";
        public const string TrainingGrade = "TrainingGrade";

        public static string UrlFor(string? type) => type switch
        {
            ExamAssigned or ExamGrade => "/Student/Exams",
            QuizAssigned or QuizGrade => "/Student/Quizzes",
            TrainingAssigned or TrainingGrade => "/Student/Trainings",
            PostCreated => "/Student/Posts",
            _ => "/Student"
        };
    }

    public interface INotificationService
    {
        // Every active student in one group.
        Task NotifyGroupAsync(int groupId, string type, string title, string message);

        // Every active student whose group belongs to one year.
        Task NotifyYearAsync(int yearId, string type, string title, string message);

        // Individual messages (e.g. "your grade is ready").
        Task NotifyStudentsAsync(IEnumerable<StudentNotification> items);
    }

    // Saves notifications and pushes them live to the student's open pages through SignalR.
    // Notifications are secondary: a failure here is logged and never breaks the action
    // (assigning an exam, saving grades, publishing a post) that triggered it.
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<ChatHub> _hub;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext db,
            IHubContext<ChatHub> hub,
            ILogger<NotificationService> logger)
        {
            _db = db;
            _hub = hub;
            _logger = logger;
        }

        public async Task NotifyGroupAsync(int groupId, string type, string title, string message)
        {
            try
            {
                var userIds = await ActiveStudentUsers()
                    .Where(e => e.GroupId == groupId)
                    .Select(e => e.Student.ApplicationUserId)
                    .Distinct()
                    .ToListAsync();

                await SendAsync(userIds.Select(id => (id, type, title, message)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not notify group {GroupId}.", groupId);
            }
        }

        public async Task NotifyYearAsync(int yearId, string type, string title, string message)
        {
            try
            {
                var userIds = await ActiveStudentUsers()
                    .Where(e => e.Group.YearId == yearId)
                    .Select(e => e.Student.ApplicationUserId)
                    .Distinct()
                    .ToListAsync();

                await SendAsync(userIds.Select(id => (id, type, title, message)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not notify year {YearId}.", yearId);
            }
        }

        public async Task NotifyStudentsAsync(IEnumerable<StudentNotification> items)
        {
            try
            {
                var list = items.ToList();
                if (list.Count == 0)
                    return;

                var studentIds = list.Select(i => i.StudentId).Distinct().ToList();
                var userByStudent = await _db.Students
                    .AsNoTracking()
                    .Where(s => studentIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, s => s.ApplicationUserId);

                await SendAsync(list
                    .Where(i => userByStudent.ContainsKey(i.StudentId))
                    .Select(i => (userByStudent[i.StudentId], i.Type, i.Title, i.Message)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send student notifications.");
            }
        }

        private IQueryable<Enrollment> ActiveStudentUsers() =>
            _db.Enrollments
                .AsNoTracking()
                .Where(e => e.Status == "Active" &&
                            e.LeftAt == null &&
                            e.Student.Status == "Active");

        private async Task SendAsync(IEnumerable<(string UserId, string Type, string Title, string Message)> items)
        {
            var list = items.ToList();
            if (list.Count == 0)
                return;

            var now = DateTime.UtcNow;
            var entities = list.Select(i => new Notification
            {
                UserId = i.UserId,
                Type = i.Type,
                Title = i.Title,
                Message = i.Message,
                CreatedAt = now,
                IsRead = false
            }).ToList();

            try
            {
                _db.Notifications.AddRange(entities);
                await _db.SaveChangesAsync();

                var userIds = entities.Select(e => e.UserId).Distinct().ToList();
                var unread = await _db.Notifications
                    .AsNoTracking()
                    .Where(n => userIds.Contains(n.UserId) && !n.IsRead)
                    .GroupBy(n => n.UserId)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count);

                foreach (var n in entities)
                {
                    await _hub.Clients
                        .Group($"user_{n.UserId}")
                        .SendAsync("ReceiveNotification", new
                        {
                            id = n.Id,
                            type = n.Type,
                            title = n.Title,
                            message = n.Message,
                            createdAt = n.CreatedAt,
                            unreadCount = unread.GetValueOrDefault(n.UserId)
                        });
                }
            }
            catch
            {
                // Keep the shared DbContext clean if saving failed halfway.
                foreach (var n in entities)
                {
                    if (_db.Entry(n).State == EntityState.Added)
                        _db.Entry(n).State = EntityState.Detached;
                }

                throw;
            }
        }
    }
}
