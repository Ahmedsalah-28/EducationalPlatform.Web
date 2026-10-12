namespace EducationalPlatform.Models
{
    public class ConversationParticipant
    {
        public int ConversationId { get; set; }

        public string UserId { get; set; } = null!;


        public Conversation Conversation { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;
    }
}