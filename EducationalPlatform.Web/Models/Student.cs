namespace EducationalPlatform.Models
{
    public class Student
    {
        public int Id { get; set; }

        public string ApplicationUserId { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }

        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ApplicationUser ApplicationUser { get; set; } = null!;

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; }
            = new List<AttendanceRecord>();

        public ICollection<TrainingRecord> TrainingRecords { get; set; }
            = new List<TrainingRecord>();

        public ICollection<TrainingAttempt> TrainingAttempts { get; set; }
            = new List<TrainingAttempt>();

        public ICollection<ExamResult> ExamResults { get; set; }
            = new List<ExamResult>();

        public ICollection<QuizAttempt> QuizAttempts { get; set; }
            = new List<QuizAttempt>();
    }
}