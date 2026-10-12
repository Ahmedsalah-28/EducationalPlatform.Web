namespace EducationalPlatform.Models
{
    public class QuizAssignment
    {
        public int Id { get; set; }

        public int QuizId { get; set; }

        public int GroupId { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        // Navigation
        public Quiz Quiz { get; set; } = null!;

        public Group Group { get; set; } = null!;

        public ICollection<QuizAttempt> QuizAttempts { get; set; }
            = new List<QuizAttempt>();
    }
}
