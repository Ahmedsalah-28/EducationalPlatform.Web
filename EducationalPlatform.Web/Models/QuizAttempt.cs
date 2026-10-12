namespace EducationalPlatform.Models
{
    public class QuizAttempt
    {
        public int Id { get; set; }

        public int QuizAssignmentId { get; set; }

        public int StudentId { get; set; }

        public decimal Score { get; set; }

        public decimal MaxScore { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        // Navigation
        public QuizAssignment QuizAssignment { get; set; } = null!;

        public Student Student { get; set; } = null!;
    }
}
