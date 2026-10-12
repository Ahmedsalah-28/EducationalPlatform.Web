namespace EducationalPlatform.Models
{
    public class ExamAssignment
    {
        public int Id { get; set; }

        public int ExamId { get; set; }

        public int GroupId { get; set; }

        public DateTime ExamDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        // Navigation
        public Exam Exam { get; set; } = null!;

        public Group Group { get; set; } = null!;

        public ICollection<ExamResult> ExamResults { get; set; }
            = new List<ExamResult>();
    }
}
