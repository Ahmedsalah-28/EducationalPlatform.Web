namespace EducationalPlatform.Models
{
    public class ClassSession
    {
        public int Id { get; set; }

        public int GroupId { get; set; }

        public DateTime Date { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string Status { get; set; } = "Scheduled";

        // Navigation
        public Group Group { get; set; } = null!;

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; }
            = new List<AttendanceRecord>();
    }
}