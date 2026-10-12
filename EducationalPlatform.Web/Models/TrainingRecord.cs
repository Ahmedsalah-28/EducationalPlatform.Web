namespace EducationalPlatform.Models
{
    public class TrainingRecord
    {
        public int Id { get; set; }

        public int TrainingAssignmentId { get; set; }

        public int StudentId { get; set; }

        public decimal Score { get; set; }

        public decimal MaxScore { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public string RecordedByUserId { get; set; } = null!;

        // Navigation
        public TrainingAssignment TrainingAssignment { get; set; } = null!;

        public Student Student { get; set; } = null!;

        public ApplicationUser RecordedByUser { get; set; } = null!;
    }
}
