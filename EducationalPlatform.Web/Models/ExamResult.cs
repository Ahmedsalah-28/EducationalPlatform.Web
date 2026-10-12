namespace EducationalPlatform.Models
{
    public class ExamResult
    {
        public int Id { get; set; }

        public int ExamAssignmentId { get; set; }

        public int StudentId { get; set; }

        public decimal Score { get; set; }

        public decimal MaxScore { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public string RecordedByUserId { get; set; } = null!;

        // Navigation
        public ExamAssignment ExamAssignment { get; set; } = null!;

        public Student Student { get; set; } = null!;

        public ApplicationUser RecordedByUser { get; set; } = null!;
    }
}
