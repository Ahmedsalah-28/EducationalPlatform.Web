namespace EducationalPlatform.Models
{
    public class AttendanceRecord
    {
        public int Id { get; set; }

        public int ClassSessionId { get; set; }

        public int StudentId { get; set; }

        public string Status { get; set; } = "Present";

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        public string RecordedByUserId { get; set; } = null!;

        // Navigation
        public ClassSession ClassSession { get; set; } = null!;

        public Student Student { get; set; } = null!;

        public ApplicationUser RecordedByUser { get; set; } = null!;
    }
}
