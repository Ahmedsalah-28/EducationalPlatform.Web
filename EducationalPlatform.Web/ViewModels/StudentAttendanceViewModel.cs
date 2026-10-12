
namespace EducationalPlatform.ViewModels
{
    public class StudentAttendanceViewModel
    {
        public int SessionId { get; set; }

        public string YearName { get; set; } = string.Empty;

        public string GroupName { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string SessionStatus { get; set; } = string.Empty;

        public string AttendanceStatus { get; set; } = string.Empty;
    }
}