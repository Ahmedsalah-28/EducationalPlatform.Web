namespace EducationalPlatform.ViewModels
{
    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; } = string.Empty;

        public bool HasEnrollment { get; set; }
        public string YearName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;

        // Attendance summary
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public int ExcusedCount { get; set; }
        public int NotRecordedCount { get; set; }
        public int CancelledCount { get; set; }

        // Present + Late out of all recorded sessions (null when nothing is recorded yet)
        public int? AttendanceRate { get; set; }

        // Next session
        public DateTime? NextSessionDate { get; set; }
        public TimeSpan? NextSessionStart { get; set; }
        public TimeSpan? NextSessionEnd { get; set; }

        public List<StudentDashboardEvent> UpcomingExams { get; set; } = new();
        public List<StudentDashboardEvent> UpcomingQuizzes { get; set; } = new();
        public List<StudentDashboardGrade> LatestGrades { get; set; } = new();
        public List<StudentDashboardPost> LatestPosts { get; set; } = new();
    }

    public class StudentDashboardEvent
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        // Quizzes only
        public bool IsOpen { get; set; }
        public bool Submitted { get; set; }
    }

    public class StudentDashboardGrade
    {
        public string Kind { get; set; } = string.Empty;   // Exam / Quiz / Training
        public string Title { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public DateTime Date { get; set; }
    }

    public class StudentDashboardPost
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
