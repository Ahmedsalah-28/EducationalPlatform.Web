namespace EducationalPlatform.ViewModels
{
    public class AssistantDashboardViewModel
    {
        public string AssistantName { get; set; } = "Assistant";
        public int TotalYears { get; set; }
        public int TotalGroups { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTrainings { get; set; }
        public int TotalExams { get; set; }
        public int TotalQuizzes { get; set; }
        public int PendingExamResults { get; set; }
        public int PendingQuizAttempts { get; set; }
        public List<AssistantUpcomingItem> UpcomingItems { get; set; } = new();
    }

    public class AssistantUpcomingItem
    {
        public string Kind { get; set; } = "";
        public string Title { get; set; } = "";
        public string YearName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public DateTime Date { get; set; }
        public string Time { get; set; } = "";
        public string Controller { get; set; } = "";
        public int AssignmentId { get; set; }
    }
}
