namespace EducationalPlatform.Models
{
    public class Group
    {
        public int Id { get; set; }

        public int YearId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Year Year { get; set; } = null!;

        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();

        public ICollection<GroupSchedule> GroupSchedules { get; set; }
            = new List<GroupSchedule>();

        public ICollection<ClassSession> ClassSessions { get; set; }
            = new List<ClassSession>();

        public ICollection<TrainingAssignment> TrainingAssignments { get; set; }
            = new List<TrainingAssignment>();

        public ICollection<ExamAssignment> ExamAssignments { get; set; }
            = new List<ExamAssignment>();

        public ICollection<QuizAssignment> QuizAssignments { get; set; }
            = new List<QuizAssignment>();

        public ICollection<RegistrationRequest> RegistrationRequests { get; set; }
            = new List<RegistrationRequest>();
    }
}