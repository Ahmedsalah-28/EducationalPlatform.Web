namespace EducationalPlatform.Models
{
    public class Assistant
    {
        public int Id { get; set; }

        public string ApplicationUserId { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ApplicationUser ApplicationUser { get; set; } = null!;

        public ICollection<AssistantYear> AssistantYears { get; set; }
            = new List<AssistantYear>();
    }
}