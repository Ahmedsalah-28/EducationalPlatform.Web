namespace EducationalPlatform.Models
{
    public class TrainingAttempt
    {
        public int Id { get; set; }

        public int TrainingAssignmentId { get; set; }

        public int StudentId { get; set; }

        public decimal Score { get; set; }

        public decimal MaxScore { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        // Navigation
        public TrainingAssignment TrainingAssignment { get; set; } = null!;

        public Student Student { get; set; } = null!;
    }
}