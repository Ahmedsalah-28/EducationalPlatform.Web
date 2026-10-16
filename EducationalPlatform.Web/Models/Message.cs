namespace EducationalPlatform.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }

        public string SenderUserId { get; set; } = null!;

        public string Content { get; set; } = null!;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public Conversation Conversation { get; set; } = null!;

        public ApplicationUser SenderUser { get; set; } = null!;
    }
}