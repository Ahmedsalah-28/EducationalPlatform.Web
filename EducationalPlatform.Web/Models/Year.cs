namespace EducationalPlatform.Models
{
    public class Year
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Group> Groups { get; set; }
            = new List<Group>();

        public ICollection<AssistantYear> AssistantYears { get; set; }
            = new List<AssistantYear>();

        public ICollection<Post> Posts { get; set; }
            = new List<Post>();
    }
}