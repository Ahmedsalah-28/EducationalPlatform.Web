namespace EducationalPlatform.Models
{
    public class Exam
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public decimal MaximumGrade { get; set; }

        public decimal PassingPercentage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string CreatedByUserId { get; set; } = null!;

        // Navigation
        public ApplicationUser CreatedByUser { get; set; } = null!;

        public ICollection<ExamAssignment> ExamAssignments { get; set; }
            = new List<ExamAssignment>();
    }
}