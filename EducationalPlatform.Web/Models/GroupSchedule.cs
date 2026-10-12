namespace EducationalPlatform.Models
{
    public class GroupSchedule
    {
        public int Id { get; set; }

        public int GroupId { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public Group Group { get; set; } = null!;
    }
}