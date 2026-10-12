namespace EducationalPlatform.Models
{
    public class Quiz
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public decimal MaximumGrade { get; set; }

        public int DurationMinutes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedByUserId { get; set; } = null!;

        // Navigation
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<QuizAssignment> QuizAssignments { get; set; }
            = new List<QuizAssignment>();
    }
}
