
using EducationalPlatform.Data;
using EducationalPlatform.Models;
using EducationalPlatform.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlatform.Web.Controllers
{
    [Authorize(Roles = "Student,Assistant")]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ChatHub> _hub;

        public ChatController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IHubContext<ChatHub> hub)
        {
            _db = db;
            _userManager = userManager;
            _hub = hub;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Total unread messages for the sidebar badge.
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            return Json(new { count = await GetUnreadCountAsync(user.Id) });
        }

        private Task<int> GetUnreadCountAsync(string userId)
        {
            return _db.Messages.CountAsync(m =>
                m.SenderUserId != userId &&
                !m.IsRead &&
                m.Conversation.Participants.Any(p => p.UserId == userId));
        }



        [HttpGet]
        public async Task<IActionResult> Conversations()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var conversations = await _db.Conversations
                .AsNoTracking()
                .Include(c => c.Participants)
                .Include(c => c.Messages)
                .Where(c => c.Participants.Any(p => p.UserId == user.Id))
                .ToListAsync();

            var otherUserIds = conversations
                .Select(c => c.Participants
                    .FirstOrDefault(p => p.UserId != user.Id)?.UserId)
                .Where(id => id != null)
                .Select(id => id!)
                .Distinct()
                .ToList();

            var assistants = await _db.Assistants
                .AsNoTracking()
                .Where(a => otherUserIds.Contains(a.ApplicationUserId))
                .Select(a => new { a.Id, a.ApplicationUserId, a.FullName })
                .ToListAsync();

            var students = await _db.Students
                .AsNoTracking()
                .Where(s => otherUserIds.Contains(s.ApplicationUserId))
                .Select(s => new { s.Id, s.ApplicationUserId, s.FullName })
                .ToListAsync();

            var result = new List<ConversationListDto>();

            foreach (var conversation in conversations)
            {
                var otherUserId = conversation.Participants
                    .FirstOrDefault(p => p.UserId != user.Id)?.UserId;

                if (otherUserId == null)
                    continue;

                var assistant = assistants.FirstOrDefault(
                    a => a.ApplicationUserId == otherUserId);

                var student = students.FirstOrDefault(
                    s => s.ApplicationUserId == otherUserId);

                string contactName;
                string contactRole;
                int contactId;

                if (assistant != null)
                {
                    contactName = assistant.FullName;
                    contactRole = "Assistant";
                    contactId = assistant.Id;
                }
                else if (student != null)
                {
                    contactName = student.FullName;
                    contactRole = "Student";
                    contactId = student.Id;
                }
                else
                {
                    continue;
                }

                var lastMessage = conversation.Messages
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();

                var unreadCount = conversation.Messages.Count(m =>
                    m.SenderUserId != user.Id && !m.IsRead);

                result.Add(new ConversationListDto(
                    conversation.Id,
                    contactId,
                    contactName,
                    contactRole,
                    lastMessage?.Content,
                    lastMessage?.SentAt,
                    unreadCount,
                    conversation.CreatedAt));
            }

            return Json(result
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToList());
        }

        public sealed record ConversationListDto(
            int ConversationId,
            int ContactId,
            string ContactName,
            string ContactRole,
            string? LastMessage,
            DateTime? LastMessageAt,
            int UnreadCount,
            DateTime CreatedAt);


        // Return contacts that the current user is allowed to contact.
        [HttpGet]

        public async Task<IActionResult> Contacts()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            if (User.IsInRole("Student"))
            {
                var student = await _db.Students
                    .SingleOrDefaultAsync(s =>
                        s.ApplicationUserId == user.Id &&
                        s.Status == "Active");

                if (student == null)
                    return Forbid();

                var activeYearIds = await _db.Enrollments
                    .Where(e =>
                        e.StudentId == student.Id &&
                        e.Status == "Active" &&
                        e.LeftAt == null &&
                        e.Group.IsActive &&
                        e.Group.Year.IsActive)
                    .Select(e => e.Group.YearId)
                    .Distinct()
                    .ToListAsync();

                var contacts = await _db.Assistants
                    .Where(a =>
                        a.Status == "Active" &&
                        a.AssistantYears.Any(ay =>
                            activeYearIds.Contains(ay.YearId) &&
                            ay.Year.IsActive))
                    .Select(a => new ContactDto(
                        a.Id,
                        a.FullName,
                        "Assistant"))
                    .ToListAsync();

                return Json(contacts);
            }

            if (User.IsInRole("Assistant"))
            {
                var assistant = await _db.Assistants
                    .SingleOrDefaultAsync(a =>
                        a.ApplicationUserId == user.Id &&
                        a.Status == "Active");

                if (assistant == null)
                    return Forbid();

                var assignedYearIds = await _db.AssistantYears
                    .Where(ay =>
                        ay.AssistantId == assistant.Id &&
                        ay.Year.IsActive)
                    .Select(ay => ay.YearId)
                    .Distinct()
                    .ToListAsync();

                var contacts = await _db.Students
                    .Where(s =>
                        s.Status == "Active" &&
                        s.Enrollments.Any(e =>
                            e.Status == "Active" &&
                            e.LeftAt == null &&
                            e.Group.IsActive &&
                            e.Group.Year.IsActive &&
                            assignedYearIds.Contains(e.Group.YearId)))
                    .Select(s => new ContactDto(
                        s.Id,
                        s.FullName,
                        "Student",
                        s.Enrollments
                            .Where(e =>
                                e.Status == "Active" &&
                                e.LeftAt == null &&
                                e.Group.IsActive &&
                                e.Group.Year.IsActive &&
                                assignedYearIds.Contains(e.Group.YearId))
                            .Select(e => e.Group.Year.Name)
                            .FirstOrDefault(),
                        s.Enrollments
                            .Where(e =>
                                e.Status == "Active" &&
                                e.LeftAt == null &&
                                e.Group.IsActive &&
                                e.Group.Year.IsActive &&
                                assignedYearIds.Contains(e.Group.YearId))
                            .Select(e => e.Group.Name)
                            .FirstOrDefault()
                    ))
                    .ToListAsync();

                return Json(contacts);
            }

            return Forbid();
        }
        // Start a conversation or return the existing one.
        // targetId is Assistant.Id for students, Student.Id for assistants.
        [HttpPost]
        public async Task<IActionResult> StartConversation(int targetId)
        {
            if (targetId <= 0)
                return BadRequest(new { message = "Invalid contact." });

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            string studentUserId;
            string assistantUserId;

            if (User.IsInRole("Student"))
            {
                var student = await _db.Students
                    .SingleOrDefaultAsync(s =>
                        s.ApplicationUserId == user.Id &&
                        s.Status == "Active");

                var assistant = await _db.Assistants
                    .SingleOrDefaultAsync(a =>
                        a.Id == targetId &&
                        a.Status == "Active");

                if (student == null || assistant == null)
                    return NotFound(new { message = "Contact not found." });

                studentUserId = student.ApplicationUserId;
                assistantUserId = assistant.ApplicationUserId;
            }
            else if (User.IsInRole("Assistant"))
            {
                var assistant = await _db.Assistants
                    .SingleOrDefaultAsync(a =>
                        a.ApplicationUserId == user.Id &&
                        a.Status == "Active");

                var student = await _db.Students
                    .SingleOrDefaultAsync(s =>
                        s.Id == targetId &&
                        s.Status == "Active");

                if (student == null || assistant == null)
                    return NotFound(new { message = "Contact not found." });

                studentUserId = student.ApplicationUserId;
                assistantUserId = assistant.ApplicationUserId;
            }
            else
            {
                return Forbid();
            }

            // Check that the student and assistant share an eligible year.
            var activeYearIds = await _db.Enrollments
                .Where(e =>
                    e.Student.ApplicationUserId == studentUserId &&
                    e.Student.Status == "Active" &&
                    e.Status == "Active" &&
                    e.LeftAt == null &&
                    e.Group.IsActive &&
                    e.Group.Year.IsActive)
                .Select(e => e.Group.YearId)
                .Distinct()
                .ToListAsync();

            var allowed = activeYearIds.Count > 0 &&
                await _db.AssistantYears.AnyAsync(ay =>
                    ay.Assistant.ApplicationUserId == assistantUserId &&
                    ay.Assistant.Status == "Active" &&
                    ay.Year.IsActive &&
                    activeYearIds.Contains(ay.YearId));

            if (!allowed)
                return Forbid();

            // Find an existing conversation for this exact pair.
            var existingId = await _db.Conversations
                .Where(c =>
                    c.Participants.Count() == 2 &&
                    c.Participants.Any(p => p.UserId == studentUserId) &&
                    c.Participants.Any(p => p.UserId == assistantUserId))
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();

            if (existingId.HasValue)
                return Json(new { conversationId = existingId.Value });

            var conversation = new Conversation();

            conversation.Participants.Add(new ConversationParticipant
            {
                UserId = studentUserId
            });

            conversation.Participants.Add(new ConversationParticipant
            {
                UserId = assistantUserId
            });

            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();

            return Json(new { conversationId = conversation.Id });
        }

        // Load messages and mark incoming messages as read.
        [HttpGet]
        public async Task<IActionResult> Messages(int conversationId)
        {
            if (conversationId <= 0)
                return BadRequest();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var isParticipant = await _db.ConversationParticipants
                .AnyAsync(p =>
                    p.ConversationId == conversationId &&
                    p.UserId == user.Id);

            if (!isParticipant)
                return NotFound();

            var incomingMessages = await _db.Messages
                .Where(m =>
                    m.ConversationId == conversationId &&
                    m.SenderUserId != user.Id &&
                    !m.IsRead)
                .ToListAsync();

            foreach (var message in incomingMessages)
                message.IsRead = true;

            if (incomingMessages.Count > 0)
            {
                await _db.SaveChangesAsync();

                // Tell the user's open pages (any tab) that the unread count dropped.
                await _hub.Clients
                    .Group($"user_{user.Id}")
                    .SendAsync("UnreadCountChanged", new
                    {
                        count = await GetUnreadCountAsync(user.Id)
                    });
            }

            var messages = await _db.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .Select(m => new MessageDto(
                    m.Id,
                    m.SenderUserId,
                    m.Content,
                    m.SentAt,
                    m.IsRead,
                    m.SenderUserId == user.Id))
                .ToListAsync();

            return Json(messages);
        }

        // Send a message to a conversation the user belongs to.
        [HttpPost]
        public async Task<IActionResult> SendMessage(
            int conversationId,
            string content)
        {
            if (conversationId <= 0 || string.IsNullOrWhiteSpace(content))
                return BadRequest(new { message = "Message cannot be empty." });

            content = content.Trim();

            if (content.Length > 5000)
                return BadRequest(new { message = "Message is too long." });

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var participantIds = await _db.ConversationParticipants
                .Where(p => p.ConversationId == conversationId)
                .Select(p => p.UserId)
                .ToListAsync();

            if (participantIds.Count != 2 ||
                !participantIds.Contains(user.Id))
            {
                return NotFound();
            }

            var message = new Message
            {
                ConversationId = conversationId,
                SenderUserId = user.Id,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            var dto = new MessageDto(
                message.Id,
                message.SenderUserId,
                message.Content,
                message.SentAt,
                message.IsRead,
                true);

            // Notify both participants, including the sender's other tabs.
            foreach (var participantId in participantIds)
            {
                await _hub.Clients
                    .Group($"user_{participantId}")
                    .SendAsync("ReceiveMessage", new
                    {
                        conversationId,
                        message = dto
                    });
            }

            return Json(dto);
        }

        public record ContactDto(
            int Id,
            string FullName,
            string Role,
            string? YearName = null,
            string? GroupName = null
        );
        public sealed record MessageDto(
            int Id,
            string SenderUserId,
            string Content,
            DateTime SentAt,
            bool IsRead,
            bool IsMine);
    }
}
