namespace EducationalPlatform.ViewModels
{
    public class StudentExamRow
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal MaximumGrade { get; set; }
        public decimal PassingPercentage { get; set; }

        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }

        // Passed / Failed / Upcoming / Not graded yet
        public string Status { get; set; } = string.Empty;
    }

    public class StudentQuizRow
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public int DurationMinutes { get; set; }
        public decimal MaximumGrade { get; set; }

        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }

        // Upcoming / Open / In progress / Awaiting grading / Graded / Missed
        public string Status { get; set; } = string.Empty;
    }

    public class StudentTrainingRow
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = string.Empty;   // InClass / Online
        public DateTime AssignedDate { get; set; }
        public decimal MaximumGrade { get; set; }

        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }

        // Graded / Awaiting grading / In progress / Not graded yet
        public string Status { get; set; } = string.Empty;
    }
}
