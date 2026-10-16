using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EducationalPlatform.ViewModels
{
    public class ExamFormViewModel
    {
        public int Id { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = "";
        [StringLength(1000)] public string? Description { get; set; }
        [Range(typeof(decimal), "1", "999.99")] public decimal MaximumGrade { get; set; } = 100;
        [Range(typeof(decimal), "0", "100")] public decimal PassingPercentage { get; set; } = 50;
    }

    public class QuizFormViewModel
    {
        public int Id { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = "";
        [StringLength(1000)] public string? Description { get; set; }
        [Range(typeof(decimal), "1", "999.99")] public decimal MaximumGrade { get; set; } = 10;
        [Range(1, 600)] public int DurationMinutes { get; set; } = 15;
    }

    public class AssignExamViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = "";
        [Display(Name = "Year"), Range(1, int.MaxValue, ErrorMessage = "Please choose a year.")] public int YearId { get; set; }
        [Display(Name = "Group"), Range(1, int.MaxValue, ErrorMessage = "Please choose a group.")] public int GroupId { get; set; }
        [DataType(DataType.Date), Required] public DateTime ExamDate { get; set; } = DateTime.Today;
        [Required] public TimeSpan StartTime { get; set; } = new(9, 0, 0);
        [Required] public TimeSpan EndTime { get; set; } = new(10, 0, 0);
        public List<SelectListItem> Years { get; set; } = new();
        public List<SelectListItem> Groups { get; set; } = new();
    }

    public class AssignQuizViewModel
    {
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = "";
        [Range(1, int.MaxValue)] public int YearId { get; set; }
        [Range(1, int.MaxValue)] public int GroupId { get; set; }
        [Required] public DateTime StartDateTime { get; set; } = DateTime.Now.AddHours(1);
        [Required] public DateTime EndDateTime { get; set; } = DateTime.Now.AddHours(2);
        public List<SelectListItem> Years { get; set; } = new();
        public List<SelectListItem> Groups { get; set; } = new();
    }

    public class ExamAssignmentListItem
    {
        public int Id { get; set; }
        public string ExamTitle { get; set; } = "";
        public string YearName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public DateTime ExamDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal MaximumGrade { get; set; }
        public int GradedCount { get; set; }
        public int StudentCount { get; set; }
    }

    public class QuizAssignmentListItem
    {
        public int Id { get; set; }
        public string QuizTitle { get; set; } = "";
        public string YearName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public decimal MaximumGrade { get; set; }
        public int SubmittedCount { get; set; }
        public int StudentCount { get; set; }
    }

    public class GradeExamViewModel
    {
        public int AssignmentId { get; set; }
        public string ExamTitle { get; set; } = "";
        public string YearName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public decimal MaximumGrade { get; set; }
        public List<ExamGradeStudent> Students { get; set; } = new();
    }

    public class ExamGradeStudent
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = "";
        public decimal? Score { get; set; }
    }

    public class GradeQuizViewModel
    {
        public int AssignmentId { get; set; }
        public string QuizTitle { get; set; } = "";
        public string YearName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public decimal MaximumGrade { get; set; }
        public List<QuizGradeStudent> Students { get; set; } = new();
    }

    public class QuizGradeStudent
    {
        public int AttemptId { get; set; }          // 0 = no attempt recorded yet
        public int StudentId { get; set; }
        public string FullName { get; set; } = "";
        public decimal? Score { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
}
