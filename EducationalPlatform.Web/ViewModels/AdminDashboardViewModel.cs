namespace EducationalPlatform.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalYears { get; set; }
        public int TotalGroups { get; set; }
        public int TotalStudents { get; set; }
        public int TotalAssistants { get; set; }
        public int TotalSchedules { get; set; }
        public int TotalSessions { get; set; }
        public int PendingRequests { get; set; }

        public List<TodaySessionItem> TodaySessions { get; set; } = new();
    }

    public class TodaySessionItem
    {
        public int SessionId { get; set; }
        public string GroupName { get; set; } = "";
        public string YearName { get; set; } = "";
        public string Time { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
